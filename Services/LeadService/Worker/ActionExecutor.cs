using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.TgEngineService;

namespace states.Services.LeadService.Worker;

public class ActionExecutor : IActionExecutor
{
    private readonly ITGEngineClient tgengine;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly ILogger<ActionExecutor> logger;

    public ActionExecutor(
        ITGEngineClient tgengine,
        ILeadStateRepository leadStateRepository,
        ILogger<ActionExecutor> logger)
    {
        this.tgengine = tgengine;
        this.leadStateRepository = leadStateRepository;
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
        await tgengine.SendPreset(
            task.TenantId,
            task.SpaceId,
            task.BotId,
            task.ChatId,
            task.PresetId,
            ct);
    }

    private async Task ExecuteManageTag(ManageTagActionTaskDocument task, CancellationToken ct)
    {
        await leadStateRepository.ManageTag(task.LeadStateId, task.Operation, task.TagId, task.ReplacementTagId, ct);
    }
}
