using states.Mongo.Documents;

namespace states.Services.LeadService.Worker;

public class ActionExecutor : IActionExecutor
{
    private readonly ILogger<ActionExecutor> logger;

    public ActionExecutor(ILogger<ActionExecutor> logger)
    {
        this.logger = logger;
    }

    public async Task Execute(ActionTaskDocument task, CancellationToken ct)
    {
        switch (task)
        {
            case SendPresetActionTaskDocument sendPreset:
                await ExecuteSendPreset(sendPreset, ct);
                break;

            case ManageTagActionTaskDocument manageTag:
                await ExecuteManageTag(manageTag, ct);
                break;

            default:
                throw new InvalidOperationException($"Unknown action task type: {task.GetType().Name}");
        }
    }

    private async Task ExecuteSendPreset(SendPresetActionTaskDocument task, CancellationToken ct)
    {
        // TODO: вызов сервиса отправки пресетов (HTTP / gRPC)
        logger.LogInformation("Executing SendPreset: PresetId={PresetId}, NeedPin={NeedPin}, LeadState={LeadStateId}",
            task.PresetId, task.NeedPin, task.LeadStateId);

        await Task.CompletedTask;
    }

    private async Task ExecuteManageTag(ManageTagActionTaskDocument task, CancellationToken ct)
    {
        // TODO: вызов сервиса управления тегами лида
        logger.LogInformation("Executing ManageTag: Operation={Operation}, TagId={TagId}, LeadState={LeadStateId}",
            task.Operation, task.TagId, task.LeadStateId);

        await Task.CompletedTask;
    }
}
