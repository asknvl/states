using states.Mongo.Documents;
using states.Mongo.Repositories;

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
                logger.LogInformation("All actions completed for lead {LeadStateId} at node {NodeId}, transitioning",
                    task.LeadStateId, task.NodeId);

                var leadState = await leadStateRepository.GetLeadState(task.LeadStateId, ct);

                // Если executor уже переместил лид на другую ноду (например, AiRouter), не делаем повторный переход.
                if (leadState.NodeId == task.NodeId && leadState.Status == LeadFunnelStatus.Nothing)
                    await progressionService.TransitionToNextNode(task.LeadStateId, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
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
}
