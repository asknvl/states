using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services;

namespace states.Services.LeadService.Worker;

public sealed class ActionWorkerService : BackgroundService
{
    private readonly IActionTaskRepository taskRepository;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IActionExecutor actionExecutor;
    private readonly ILeadProgressionService progressionService;
    private readonly ILogger<ActionWorkerService> logger;

    private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(1);
    private readonly int maxConcurrency = 10;

    // Ретраи transient-ошибок: 30с → 1м → 2м → 4м → 8м (±20% джиттера), итого ~15 минут,
    // после чего обычный Fail (+Manual для критичных тасок).
    private const int MaxRetryAttempts = 5;

    // «Бот ещё не поднялся» — обычно гонка с деплоем tgengine: рантаймы ботов стартуют
    // не мгновенно. Ждём фиксированные 30 секунд, без экспоненты: бот либо поднимается
    // за десятки секунд, либо не поднимется вовсе (выключен, разлогинен) — и тогда лида
    // нет смысла держать, он уходит на оператора. Бюджет попыток общий с transient: на
    // деплое эти ошибки идут вперемешку (сперва отказ соединения, потом BOT_NOT_RUNNING),
    // и отдельный счётчик тут только растянул бы суммарное ожидание.
    private static readonly TimeSpan BotRestartRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryMaxDelay = TimeSpan.FromMinutes(10);

    public ActionWorkerService(
        IActionTaskRepository taskRepository,
        ILeadStateRepository leadStateRepository,
        IActionExecutor actionExecutor,
        ILeadProgressionService progressionService,
        ILogger<ActionWorkerService> logger)
    {
        this.taskRepository = taskRepository;
        this.leadStateRepository = leadStateRepository;
        this.actionExecutor = actionExecutor;
        this.progressionService = progressionService;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ActionWorkerService started");

        var semaphore = new SemaphoreSlim(maxConcurrency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await semaphore.WaitAsync(stoppingToken);

                var task = await taskRepository.ClaimNext(stoppingToken);
                if (task is null)
                {
                    semaphore.Release();
                    await Task.Delay(pollingInterval, stoppingToken);
                    continue;
                }

                _ = ProcessTask(task, semaphore, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error claiming action task");
                semaphore.Release();
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }

        logger.LogInformation("ActionWorkerService stopped");
    }

    private async Task ProcessTask(ActionTaskDocument task, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            logger.LogInformation($"ActionWorkerService Processing Task: {task}");

            await actionExecutor.Execute(task, ct);
            await taskRepository.Complete(task.Id, ct);
            await leadStateRepository.UpdateActionStatus(
                task.LeadStateId, task.NodeId, task.ActionId, ActionStatus.Completed, ct);
            await taskRepository.UnlockNext(task.LeadStateId, task.NodeId, task.Order, ct);

            logger.LogInformation("Action task {TaskId} completed for lead {LeadStateId}",
                task.Id, task.LeadStateId);

            var allDone = await leadStateRepository.AreAllActionsCompleted(task.LeadStateId, task.NodeId, ct);          //TODO в асинхронном контексте тут может быть гонка

            if (allDone)
            {
                logger.LogInformation("All actions completed for lead {LeadStateId} at node {NodeId}, applying finish status",
                    task.LeadStateId, task.NodeId);

                // Применяет FinishStatus ноды (или двигает лида дальше, если FinishStatus == Nothing).
                // Сама проверяет, что лид всё ещё на этой ноде и не был уже перемещён/переведён
                // в другой статус помимо этого пути (например, AiRouter или критичный fail).
                await progressionService.CompleteNodeActions(task.LeadStateId, task.NodeId, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (BotNotRunningException ex) when (task.Attempt < MaxRetryAttempts)
        {
            logger.LogWarning(ex,
                "Action task {TaskId} hit a bot that is not running (attempt {Attempt}/{MaxAttempts}), retry in {Delay} for lead {LeadStateId}",
                task.Id, task.Attempt + 1, MaxRetryAttempts, BotRestartRetryDelay, task.LeadStateId);

            await taskRepository.Reschedule(task.Id, DateTime.UtcNow + BotRestartRetryDelay, ct);
        }
        catch (TransientActionException ex) when (task.Attempt < MaxRetryAttempts)
        {
            var delay = ComputeRetryDelay(task.Attempt);

            logger.LogWarning(ex,
                "Action task {TaskId} transient failure (attempt {Attempt}/{MaxAttempts}), retry in {Delay} for lead {LeadStateId}",
                task.Id, task.Attempt + 1, MaxRetryAttempts, delay, task.LeadStateId);

            await taskRepository.Reschedule(task.Id, DateTime.UtcNow + delay, ct);
        }
        // Лид заблокировал бота. В Manual не уводим: писать ему некуда, оператор бесполезен,
        // а статус Blocked приедет событием деактивации бота из tgengine — Manual с ним только
        // конфликтовал бы, затирая или затираясь в зависимости от того, что запишется последним.
        catch (TelegramActionException ex) when (ex.Code == TelegramActionException.UserIsBlockedCode)
        {
            logger.LogWarning(ex,
                "Action task {TaskId} stopped: lead {LeadStateId} has blocked the bot",
                task.Id, task.LeadStateId);

            await taskRepository.Fail(task.Id, ct);
            await leadStateRepository.UpdateActionStatus(
                task.LeadStateId, task.NodeId, task.ActionId, ActionStatus.Failed, ct, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Action task {TaskId} failed", task.Id);
            await taskRepository.Fail(task.Id, ct);
            await leadStateRepository.UpdateActionStatus(
                task.LeadStateId, task.NodeId, task.ActionId, ActionStatus.Failed, ct, ex.Message);

            if (task.IsCritical)
            {
                await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Manual, ct);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static TimeSpan ComputeRetryDelay(int attempt)
    {
        var backoff = RetryBaseDelay * Math.Pow(2, attempt);
        if (backoff > RetryMaxDelay)
            backoff = RetryMaxDelay;

        // Джиттер ±20%: таски, упавшие одновременно при недоступном aiservice,
        // не ударят по нему одной пачкой при восстановлении
        var jitter = 0.8 + Random.Shared.NextDouble() * 0.4;
        return backoff * jitter;
    }
}
