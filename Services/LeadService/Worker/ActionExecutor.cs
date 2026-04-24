using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.TgEngineService;

namespace states.Services.LeadService.Worker;

public class ActionExecutor : IActionExecutor
{
    private readonly ITGEngineClient tgengine;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly ILogger<ActionExecutor> logger;

    public ActionExecutor(
        ITGEngineClient tgengine,
        ILeadStateRepository leadStateRepository,
        IFunnelRuntimeCache funnelCache,
        ILogger<ActionExecutor> logger)
    {
        this.tgengine = tgengine;
        this.leadStateRepository = leadStateRepository;
        this.funnelCache = funnelCache;
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
            task.FunnelId,            
            task.PresetId,
            ct);
    }

    private async Task ExecuteManageTag(ManageTagActionTaskDocument task, CancellationToken ct)
    {
        var funnel = funnelCache.GetFunnel(task.FunnelId);
        if (funnel is null)
            throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var tag = funnel.Tags.FirstOrDefault(t => t.Id == task.TagId);
        if (tag is null)
            throw new KeyNotFoundException($"Tag id={task.TagId} not found");

        Tag? replacementTag = null;

        if (task.ReplacementTagId is not null)
        {
            replacementTag = funnel.Tags.FirstOrDefault(t => t.Id == task.ReplacementTagId);
            if (replacementTag is null)
                throw new KeyNotFoundException($"Replacement tag id={task.TagId} not found");
        }

        await leadStateRepository.UpdateTag(
            task.LeadStateId,
            task.Operation,
            tag,
            replacementTag,
            ct);
    }
}
