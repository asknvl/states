using states.Mongo.Documents;
using states.Mongo.Repositories;

namespace states.Services.LeadService.Worker;

public sealed class PushWorkerService : BackgroundService
{
    private readonly IPushTaskRepository taskRepository;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IPushExecutor pushExecutor;
    private readonly ILogger<PushWorkerService> logger;

    private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(1);
    private readonly int maxConcurrency = 10;

    public PushWorkerService(
        IPushTaskRepository taskRepository,
        ILeadStateRepository leadStateRepository,
        IPushExecutor pushExecutor,
        ILogger<PushWorkerService> logger)
    {
        this.taskRepository = taskRepository;
        this.leadStateRepository = leadStateRepository;
        this.pushExecutor = pushExecutor;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PushWorkerService started");

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
                logger.LogError(ex, "Error claiming push task");
                semaphore.Release();
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }

        logger.LogInformation("PushWorkerService stopped");
    }

    private async Task ProcessTask(PushTaskDocument task, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            logger.LogInformation($"PushWorkerService Processing Task: {task.Id}");

            // Inactivity-пуш (AiReply-нода) отправляем, только если лид молчал весь свой delay.
            // Якорь таска = ScheduledAt - Delay; если лид писал после якоря — переносим отправку
            // на LastIncomingAt + Delay. Проверка в момент отправки самовосстанавливающаяся:
            // сколько бы сообщений ни пришло, таск просто едет вперёд, пока delay тишины не выдержан.
            if (task.IsInactivityPush)
            {
                var delay = task.Delay ?? TimeSpan.Zero;
                var leadState = await leadStateRepository.GetLeadState(task.LeadStateId, ct);

                if (leadState.LastIncomingAt is { } lastIncoming && lastIncoming > task.ScheduledAt - delay)
                {
                    var newScheduledAt = lastIncoming + delay;

                    await taskRepository.Reschedule(task.Id, newScheduledAt, ct);

                    logger.LogInformation(
                        "Push task {TaskId} rescheduled to {NewScheduledAt} for lead {LeadStateId} — lead wrote at {LastIncomingAt}",
                        task.Id, newScheduledAt, task.LeadStateId, lastIncoming);
                    return;
                }
            }

            await pushExecutor.Execute(task, ct);
            await leadStateRepository.MarkPushCompleted(task.LeadStateId, task.PushId, ct);
            await taskRepository.Complete(task.Id, ct);
            await taskRepository.UnlockNext(task.LeadStateId, task.NodeId, task.Order, ct);

            logger.LogInformation("Push task {TaskId} completed for lead {LeadStateId}",
                task.Id, task.LeadStateId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Push task {TaskId} failed", task.Id);
            await taskRepository.Fail(task.Id, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
