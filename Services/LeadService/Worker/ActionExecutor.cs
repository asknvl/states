using aiservice.Dtos.APIs.Reply;
using aiservice.Dtos.APIs.Router;
using states.Dtos.Funnels;
using states.Dtos.Nodes;
using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.AIServiceClient;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.LeadService.Routing;
using states.Services.TGEngineClient.Dtos;
using states.Services.TgEngineService;
using states.Utils;
using System.Text.Json;

namespace states.Services.LeadService.Worker;

public class ActionExecutor : IActionExecutor
{
    private readonly ITGEngineClient tgengine;
    private readonly IAIServiceClient aiServiceClient;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IActionTaskRepository actionTaskRepository;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly ILeadProgressionService progressionService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ActionExecutor> logger;

    public ActionExecutor(
        ITGEngineClient tgengine,
        IAIServiceClient aiServiceClient,
        ILeadStateRepository leadStateRepository,
        IActionTaskRepository actionTaskRepository,
        IFunnelRuntimeCache funnelCache,
        ILeadProgressionService progressionService,
        IHttpClientFactory httpClientFactory,
        ILogger<ActionExecutor> logger)
    {
        this.tgengine = tgengine;
        this.aiServiceClient = aiServiceClient;
        this.leadStateRepository = leadStateRepository;
        this.actionTaskRepository = actionTaskRepository;
        this.funnelCache = funnelCache;
        this.progressionService = progressionService;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task Execute(ActionTaskDocument task, CancellationToken ct)
    {
        switch (task)
        {
            case SendPresetActionTaskDocument sendPreset:
                logger.LogInformation("ActionExecutor Execute ExecuteSendPreset");
                await ExecuteSendPreset(sendPreset, ct);
                break;

            case ManageTagActionTaskDocument manageTag:
                logger.LogInformation("ActionExecutor Execute ExecuteManageTag");
                await ExecuteManageTag(manageTag, ct);
                break;

            case AiReplyActionTaskDocument aiReply:
                logger.LogInformation("ActionExecutor Execute ExecuteAiReply");
                await ExecuteAiReply(aiReply, ct);
                break;

            case AiRouterActionTaskDocument aiRouter:
                logger.LogInformation("ActionExecutor Execute ExecuteAiRouter");
                await ExecuteAiRouter(aiRouter, ct);
                break;

            case SendWebhookActionTaskDocument sendWebhook:
                logger.LogInformation("ActionExecutor Execute ExecuteSendWebhook");
                await ExecuteSendWebhook(sendWebhook, ct);
                break;

            default:
                throw new InvalidOperationException($"Unknown action task type: {task.GetType().Name}");
        }
    }

    private async Task ExecuteSendPreset(SendPresetActionTaskDocument task, CancellationToken ct)
    {
        var funnel = funnelCache.GetFunnel(task.FunnelId)
            ?? throw new KeyNotFoundException($"Funnel id={task.FunnelId} not found");

        var variables = funnel.Variables.Select(v => new Variable(Macros: v.Macros, Value: v.Value)).ToList();

        await tgengine.SendPreset(
            task.TenantId,
            task.SpaceId,
            task.BotId,
            task.ChatId,
            variables,
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

        var leadState = await leadStateRepository.GetLeadState(task.LeadStateId, ct);

        var aiRouterEdges = AiRouterEdgeSelector.GetEligibleEdges(flow, task.NodeId, leadState);

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
            lastMessagesNumber: 30,
            returnFromLastOutcoming: false,
            isImageDetailed: false,
            ct); //TODO сделать чтобы если уже есть такой таск, то было +1 сообщение и выбиралось количество сообщений по счетчику!!!

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
            ModelPresetId: funnel.AiRouterModelPresetId,
            Routers: routers,
            Context: context);

        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });

        logger.LogInformation(
            "AiRouter: sending route request for ChatId {ChatId}. Request: {Request}",
            task.ChatId,
            requestJson);


        var response = await aiServiceClient.RouteAsync(request, ct);

        var matchedId = response.Id;
        if (matchedId is null)
        {
            logger.LogInformation("AiRouter: no match for lead {LeadStateId}, reason: {Reason}, scheduling AI reply",
                task.LeadStateId, response.Reason);
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
            await actionTaskRepository.TryInsertAiReplyTask(replyTask, ct);
            //await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Waiting, ct); // ХЗ зачем тут добавлял
            return;
        }

        var matchedEdge = aiRouterEdges.FirstOrDefault(e => e.Id.ToString() == matchedId)
            ?? throw new InvalidOperationException($"AiRouter matched edge '{matchedId}' not found in flow.");

        logger.LogInformation("AiRouter: lead {LeadStateId} matched edge {EdgeId}, reason: {Reason}",
            task.LeadStateId, matchedEdge.Id, response.Reason);

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

        logger.LogInformation($"ActionExecutor Node={node.Data.Label}");

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

        var variables = funnel.Variables.Select(v => new Variable(Macros: v.Macros, Value: v.Value)).ToList();

        await tgengine.SendAiTextMessages(
            task.TenantId,
            task.SpaceId,
            task.BotId,
            task.ChatId,
            variables,
            response.Text,
            ct);

        logger.LogInformation("AiReply: sent reply for lead {LeadStateId}", task.LeadStateId);

        if (task.TransitionAfterReply)
            await progressionService.TransitionToNextNode(task.LeadStateId, ct); 
        //else
        //    await leadStateRepository.UpdateLeadStateStatus(task.LeadStateId, LeadFunnelStatus.Waiting, ct); //Оно и так вроде в вейтинге всегда в этом месте
    }

    private async Task ExecuteSendWebhook(SendWebhookActionTaskDocument task, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadState(task.LeadStateId, ct);

        var url = MacrosResolver.Resolve(task.Url, leadState);

        var method = task.MethodType switch
        {
            WebhookMethodType.Get => HttpMethod.Get,
            WebhookMethodType.Post => HttpMethod.Post,
            _ => throw new NotSupportedException($"Unsupported webhook method type: {task.MethodType}")
        };

        var http = httpClientFactory.CreateClient();

        HttpResponseMessage response;

        try
        {
            response = await http.SendAsync(new HttpRequestMessage(method, url), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SendWebhook failed: url={Url}", url);
            throw;
        }

        response.EnsureSuccessStatusCode();

        logger.LogInformation("SendWebhook: sent {MethodType} request to {Url} for lead {LeadStateId}",
            task.MethodType, url, task.LeadStateId);
    }
}
