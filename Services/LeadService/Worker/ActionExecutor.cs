using aiservice.Dtos.APIs.Reply;
using aiservice.Dtos.APIs.Router;
using states.Dtos.Edges;
using states.Dtos.Funnels;
using states.Dtos.Nodes;
using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.AIServiceClient;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.TgEngineService;

namespace states.Services.LeadService.Worker;

public class ActionExecutor : IActionExecutor
{
    private readonly ITGEngineClient tgengine;
    private readonly IAIServiceClient aiServiceClient;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IActionTaskRepository actionTaskRepository;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly ILeadProgressionService progressionService;
    private readonly ILogger<ActionExecutor> logger;

    public ActionExecutor(
        ITGEngineClient tgengine,
        IAIServiceClient aiServiceClient,
        ILeadStateRepository leadStateRepository,
        IActionTaskRepository actionTaskRepository,
        IFunnelRuntimeCache funnelCache,
        ILeadProgressionService progressionService,
        ILogger<ActionExecutor> logger)
    {
        this.tgengine = tgengine;
        this.aiServiceClient = aiServiceClient;
        this.leadStateRepository = leadStateRepository;
        this.actionTaskRepository = actionTaskRepository;
        this.funnelCache = funnelCache;
        this.progressionService = progressionService;
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

            case AiReplyActionTaskDocument aiReply:
                await ExecuteAiReply(aiReply, ct);
                break;

            case AiRouterActionTaskDocument aiRouter:
                await ExecuteAiRouter(aiRouter, ct);
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
        var funnel = funnelCache.GetFunnel(task.FunnelId)
            ?? throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var tag = funnel.Tags.FirstOrDefault(t => t.Id == task.TagId)
            ?? throw new KeyNotFoundException($"Tag id={task.TagId} not found");

        Tag? replacementTag = null;

        if (task.ReplacementTagId is not null)
        {
            replacementTag = funnel.Tags.FirstOrDefault(t => t.Id == task.ReplacementTagId)
                ?? throw new KeyNotFoundException($"Replacement tag id={task.ReplacementTagId} not found");
        }

        await leadStateRepository.UpdateTag(
            task.LeadStateId,
            task.Operation,
            tag,
            replacementTag!,
            ct);
    }

    private async Task ExecuteAiRouter(AiRouterActionTaskDocument task, CancellationToken ct)
    {        
        var funnel = funnelCache.GetFunnel(task.FunnelId)
            ?? throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == task.FlowId)
            ?? throw new KeyNotFoundException($"Flow id={task.FlowId} not found");

        logger.LogInformation($"AiRouter: Executing router ModelPreset={funnel.AiRouterModelPresetId}");

        var aiRouterEdges = flow.Edges
            .Where(e => e.Source == task.NodeId)
            .OfType<AiRouterEdge>()
            .ToList();

        if (aiRouterEdges.Count == 0)
        {
            logger.LogWarning("AiRouter: no outgoing edges for lead {LeadStateId} at node {NodeId}",
                task.LeadStateId, task.NodeId);
            await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Waiting, ct);
            return;
        }

        var tgMessages = await tgengine.GetContextMessages(
            task.TenantId,
            task.BotId,
            task.ChatId,
            lastMessagesNumber: 5,
            returnFromLastOutcoming: true,
            isImageDetailed: false,
            ct); //TODO сделать чтобы если уже есть такой таск, то было +1 сообщение и выбиралось количество сообщений по счетчику

        var context = tgMessages
            .Select(m => new aiservice.Dtos.APIs.Chat.ChatContextMessageDto(
                m.Role, m.Text,
                m.Image is null ? null : new aiservice.Dtos.APIs.Chat.ImageContentDto(m.Image.MimeType, m.Image.Data)))
            .ToList();

        var routers = aiRouterEdges
            .Select(e => new RouterRuleDto(e.Id.ToString(), e.Thesis))
            .ToList();

        var request = new RouteRequestDto(
            TenantId: task.TenantId,
            ChatId: task.ChatId,
            BotId: task.BotId,
            ModelPresetId: funnel.AiRouterModelPresetId, // TODO: будет браться из Funnel
            Routers: routers,
            Context: context,
            Options: new RouteOptionsDto(ReturnOnlyOne: true));

        var response = await aiServiceClient.RouteAsync(request, ct);

        var matchedId = response.Results.FirstOrDefault(r => r.Matched)?.Id;
        if (matchedId is null)
        {
            logger.LogInformation("AiRouter: no match for lead {LeadStateId}, scheduling AI reply", task.LeadStateId);
            var replyTask = new AiReplyActionTaskDocument
            {
                Id = Guid.CreateVersion7(),
                TenantId = task.TenantId,
                SpaceId = task.SpaceId,
                LeadStateId = task.LeadStateId,
                FunnelId = task.FunnelId,
                FlowId = task.FlowId,
                NodeId = task.NodeId,
                ActionId = Guid.CreateVersion7(),
                BotId = task.BotId,
                ChatId = task.ChatId,
                ScheduledAt = DateTime.UtcNow + TimeSpan.FromSeconds(funnel.ReplyDelay),
                CreatedAt = DateTime.UtcNow,
                Order = 0
            };
            await actionTaskRepository.CreateMany([replyTask], ct);
            await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Waiting, ct);
            return;
        }

        var matchedEdge = aiRouterEdges.FirstOrDefault(e => e.Id.ToString() == matchedId)
            ?? throw new InvalidOperationException($"AiRouter matched edge '{matchedId}' not found in flow.");

        logger.LogInformation("AiRouter: lead {LeadStateId} matched edge {EdgeId}", task.LeadStateId, matchedEdge.Id);

        await progressionService.ExecuteTransitionByEdge(task.LeadStateId, matchedEdge.Id, ct);
    }

    private async Task ExecuteAiReply(AiReplyActionTaskDocument task, CancellationToken ct)
    {
        var funnel = funnelCache.GetFunnel(task.FunnelId)
            ?? throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == task.FlowId)
            ?? throw new KeyNotFoundException($"Flow id={task.FlowId} not found");

        var node = flow.Nodes.FirstOrDefault(n => n.Id == task.NodeId)
            ?? throw new KeyNotFoundException($"Node id={task.NodeId} not found");

        var nodeData = (AiReplyNodeData)node.Data;

        var tgMessages = await tgengine.GetContextMessages(
            task.TenantId,
            task.BotId,
            task.ChatId,
            lastMessagesNumber: 20,
            returnFromLastOutcoming: false,
            isImageDetailed: false,
            ct);

        var context = tgMessages
            .Select(m => new aiservice.Dtos.APIs.Chat.ChatContextMessageDto(
                m.Role, m.Text,
                m.Image is null ? null : new aiservice.Dtos.APIs.Chat.ImageContentDto(m.Image.MimeType, m.Image.Data)))
            .ToList();

        var request = new ReplyRequestDto(
            TenantId: task.TenantId,
            ChatId: task.ChatId,
            BotId: task.BotId,
            ModelPresetId: funnel.AiReplyModelPresetId,
            GlobalLegend: funnel.GlobalLegend,
            Restrictions: funnel.Restrictions,
            ResponseStyle: funnel.ResponseStyle,
            Goal: nodeData.Goal,
            Requirements: nodeData.Requirements,
            Legend: nodeData.Legend,
            AdditionalInfo: nodeData.AdditionalInfo,
            Temperature: funnel.AiReplyTemperature,
            Context: context);

        var response = await aiServiceClient.ReplyAsync(request, ct);

        await tgengine.SendAiTextMessages(task.TenantId, task.SpaceId, task.BotId, task.ChatId, response.Text, ct);

        logger.LogInformation("AiReply: sent reply for lead {LeadStateId}", task.LeadStateId);

        if (task.TransitionAfterReply)
            await progressionService.TransitionToNextNode(task.LeadStateId, ct);
        else
            await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Waiting, ct);
    }
}
