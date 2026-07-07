using states.Mongo.Documents;
using states.Services.FunnelService.Runtime;
using states.Services.TGEngineClient.Dtos;
using states.Services.TgEngineService;

namespace states.Services.LeadService.Worker;

public class PushExecutor : IPushExecutor
{
    private readonly ITGEngineClient tgengine;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly ILogger<PushExecutor> logger;

    public PushExecutor(
        ITGEngineClient tgengine,
        IFunnelRuntimeCache funnelCache,
        ILogger<PushExecutor> logger)
    {
        this.tgengine = tgengine;
        this.funnelCache = funnelCache;
        this.logger = logger;
    }

    public async Task Execute(PushTaskDocument task, CancellationToken ct)
    {
        logger.LogInformation("PushExecutor Execute ExecuteSendPush");

        var funnel = funnelCache.GetFunnel(task.FunnelId)
            ?? throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var variables = funnel.Variables.Select(v => new Variable(Macros: v.Macros, Value: v.Value)).ToList();

        // Push отправляется тем же функционалом TGEngine, что и обычный пресет
        await tgengine.SendPreset(
            task.TenantId,
            task.SpaceId,
            task.BotId,
            task.ChatId,
            variables,
            task.FunnelId,
            task.PresetId,
            needPin: false,
            ct);
    }
}
