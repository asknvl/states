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

    // 30 слотов под ~40к лидов/сутки (~610к тасков): на 10 очередь копилась в пики —
    // таски подолгу висели в Pending. Пул общий для всех типов тасков.
    private readonly int maxConcurrency = 30;

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
    private static readonly TimeSpan AbandonedSweepInterval = TimeSpan.FromMinutes(5);

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

        _ = SweepAbandonedTasks(stoppingToken);

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

            await TryCompleteNode(task, ct);
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
            await FinalizeFailedTask(task, ex.Message, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Критичная таска уводит лида на оператора; некритичная не должна останавливать воронку:
    // разблокируем следующую таску ноды и даём ноде завершиться по терминальным статусам —
    // иначе хвост цепочки навсегда остаётся Waiting, а лид замирает на ноде (инцидент 2026-08-19).
    private async Task FinalizeFailedTask(ActionTaskDocument task, string? errorMessage, CancellationToken ct)
    {
        await taskRepository.Fail(task.Id, ct);
        await leadStateRepository.UpdateActionStatus(
            task.LeadStateId, task.NodeId, task.ActionId, ActionStatus.Failed, ct, errorMessage);

        if (task.IsCritical)
        {
            await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Manual, ct);
        }
        else
        {
            await taskRepository.UnlockNext(task.LeadStateId, task.NodeId, task.Order, ct);
            await TryCompleteNode(task, ct);
        }
    }

    private async Task TryCompleteNode(ActionTaskDocument task, CancellationToken ct)
    {
        var allDone = await leadStateRepository.AreAllActionsFinished(task.LeadStateId, task.NodeId, ct);          //TODO в асинхронном контексте тут может быть гонка

        if (allDone)
        {
            logger.LogInformation("All actions finished for lead {LeadStateId} at node {NodeId}, applying finish status",
                task.LeadStateId, task.NodeId);

            // Применяет FinishStatus ноды (или двигает лида дальше, если FinishStatus == Nothing).
            // Сама проверяет, что лид всё ещё на этой ноде и не был уже перемещён/переведён
            // в другой статус помимо этого пути (например, AiRouter или критичный fail).
            await progressionService.CompleteNodeActions(task.LeadStateId, task.NodeId, ct);
        }
    }

    // Финализация брошенных тасков, до которых reclaim в ClaimNext уже не дотянется (см.
    // ClaimAbandoned): без этого они висят InProgress вечно, держат Waiting-цепочку своей
    // ноды, слот unique_active_ai_reply_per_lead и лида на ноде.
    private async Task SweepAbandonedTasks(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                while (await taskRepository.ClaimAbandoned(ct) is { } task)
                {
                    logger.LogWarning(
                        "Action task {TaskId} abandoned (claimed at {ClaimedAt}, scheduled at {ScheduledAt}), failing it for lead {LeadStateId}",
                        task.Id, task.ClaimedAt, task.ScheduledAt, task.LeadStateId);

                    await FinalizeFailedTask(task, "Task abandoned by a dead worker", ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sweeping abandoned action tasks");
            }

            try
            {
                await Task.Delay(AbandonedSweepInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
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
