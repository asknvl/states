using Confluent.Kafka;
using MongoDB.Driver;
using NanoidDotNet;
using states.Dtos.Edges;
using states.Dtos.Funnels;
using states.Dtos.Leads;
using states.Dtos.Nodes;
using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.LeadService.Routing;
using System.Xml.Linq;

namespace states.Services.LeadService;

public class LeadProgressionService : ILeadProgressionService
{
    private readonly ILeadStateRepository leadStateRepository;
    private readonly IActionTaskRepository actionTaskRepository;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly IEdgeRouter edgeRouter;
    private readonly ILogger<LeadProgressionService> logger;

    public LeadProgressionService(
        ILeadStateRepository leadStateRepository,
        IActionTaskRepository actionTaskRepository,
        IFunnelRuntimeCache funnelCache,
        IEdgeRouter edgeRouter,
        ILogger<LeadProgressionService> logger)
    {
        this.leadStateRepository = leadStateRepository;
        this.actionTaskRepository = actionTaskRepository;
        this.funnelCache = funnelCache;
        this.edgeRouter = edgeRouter;
        this.logger = logger;
    }

    #region private
    private static HashSet<Guid> GetTriggeredEdgeIds(FunnelLeadState leadState) =>
        leadState.StatesLog
            .Where(s => s.ExitEdgeId.HasValue)
            .Select(s => s.ExitEdgeId!.Value)
            .ToHashSet();

    private List<ActionTaskDocument> CreateActionTasks(FunnelLeadState leadState, Dtos.Nodes.Node node)
    {
        if (leadState.FunnelId is null || leadState.FlowId is null)
            return [];

        var now = DateTime.UtcNow;
        var tasks = new List<ActionTaskDocument>();

        switch (node.Data)
        {
            case SendPresetNodeData sendPreset:
                var scheduledAt = now;
                for (var i = 0; i < sendPreset.Actions.Count; i++)
                {
                    var action = sendPreset.Actions[i];
                    if (action.Delay.HasValue)
                        scheduledAt += action.Delay.Value;

                    tasks.Add(new SendPresetActionTaskDocument
                    {
                        Id = Guid.CreateVersion7(),
                        TenantId = leadState.TenantId,
                        SpaceId = leadState.SpaceId,
                        LeadStateId = leadState.Id,
                        FunnelId = leadState.FunnelId.Value,
                        FlowId = leadState.FlowId.Value,
                        NodeId = node.Id,
                        ActionId = action.Id,
                        Order = i,
                        Status = i == 0 ? ActionStatus.Pending : ActionStatus.Waiting,
                        ScheduledAt = scheduledAt,
                        CreatedAt = now,

                        BotId = leadState.BotId,
                        ChatId = leadState.ChatId,
                        PresetId = action.PresetId,
                        NeedPin = action.NeedPin
                    });
                }
                break;

            case ManageTagNodeData manageTag:
                for (var i = 0; i < manageTag.Actions.Count; i++)
                {
                    var action = manageTag.Actions[i];
                    tasks.Add(new ManageTagActionTaskDocument
                    {
                        Id = Guid.CreateVersion7(),
                        TenantId = leadState.TenantId,
                        SpaceId = leadState.SpaceId,
                        LeadStateId = leadState.Id,
                        FunnelId = leadState.FunnelId.Value,
                        FlowId = leadState.FlowId.Value,
                        NodeId = node.Id,
                        ActionId = action.Id,
                        Order = i,
                        Status = i == 0 ? ActionStatus.Pending : ActionStatus.Waiting,
                        CreatedAt = now,

                        Operation = action.Operation,
                        TagId = action.TagId,
                        ReplacementTagId = action.ReplacementTagId
                    });
                }
                break;

                case AiReplyNodeData aiReply:

                var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value);

                if (funnel is not null)
                {
                    var flow = funnel.Flows.FirstOrDefault(f => f.Id == leadState.FlowId);
                    var triggeredEdgeIds = GetTriggeredEdgeIds(leadState);
                    var aiRouterEdges = flow?.Edges
                        .Where(e => e.Source == node.Id)
                        .OfType<AiRouterEdge>()
                        .Where(e => !e.TriggerOnce || !triggeredEdgeIds.Contains(e.Id))
                        .ToList() ?? [];

                    if (aiRouterEdges.Count > 0)
                    {
                        // У ноды есть роутеры — сначала проверяем их по уже имеющемуся контексту,
                        // и только если ничего не подошло, ExecuteAiRouter задаст вопрос AiReply.
                        tasks.Add(new AiRouterActionTaskDocument
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = leadState.TenantId,
                            SpaceId = leadState.SpaceId,
                            LeadStateId = leadState.Id,
                            FunnelId = leadState.FunnelId.Value,
                            FlowId = leadState.FlowId.Value,
                            NodeId = node.Id,
                            ActionId = Guid.CreateVersion7(),
                            Order = 0,
                            Status = ActionStatus.Pending,
                            CreatedAt = now,

                            BotId = leadState.BotId,
                            ChatId = leadState.ChatId,
                            ScheduledAt = now
                        });
                    }
                    else
                    {
                        tasks.Add(new AiReplyActionTaskDocument
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = leadState.TenantId,
                            SpaceId = leadState.SpaceId,
                            LeadStateId = leadState.Id,
                            FunnelId = leadState.FunnelId.Value,
                            FlowId = leadState.FlowId.Value,
                            NodeId = node.Id,
                            ActionId = Guid.CreateVersion7(),
                            Order = 0,
                            Status = ActionStatus.Pending,
                            CreatedAt = now,

                            BotId = leadState.BotId,
                            ChatId = leadState.ChatId,
                            ScheduledAt = now + TimeSpan.FromSeconds(funnel.ReplyDelay),

                            TransitionAfterReply = false
                        });
                    }

                    //tasks.Add(new AiReplyActionTaskDocument
                    //{
                    //    Id = Guid.CreateVersion7(),
                    //    TenantId = leadState.TenantId,
                    //    SpaceId = leadState.SpaceId,
                    //    LeadStateId = leadState.Id,
                    //    FunnelId = leadState.FunnelId.Value,
                    //    FlowId = leadState.FlowId.Value,
                    //    NodeId = node.Id,
                    //    ActionId = Guid.CreateVersion7(),
                    //    Order = 0,
                    //    Status = ActionStatus.Pending,
                    //    CreatedAt = now,

                    //    BotId = leadState.BotId,
                    //    ChatId = leadState.ChatId,
                    //    ScheduledAt = now + TimeSpan.FromSeconds(funnel.ReplyDelay),

                    //    TransitionAfterReply = false
                    //});
                }
                break;
        }

        return tasks;
    }
    #endregion

    #region public   
    public async Task EnterFunnel(EnterFunnelRequest request, CancellationToken ct)
    {
        Funnel? funnel = null;
        Flow? flow = null;
        Dtos.Nodes.Node? node = null;

        if (request.FunnelId.HasValue)
            funnel = funnelCache.GetFunnel(request.FunnelId.Value);

        if (request.FlowId.HasValue)
            flow = funnel?.Flows.FirstOrDefault(f => f.Id == request.FlowId.Value);

        if (request.NodeId.HasValue)
            node = flow?.Nodes.FirstOrDefault(n => n.Id == request.NodeId.Value);

        var leadState = new FunnelLeadState
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            SpaceId = request.SpaceId,
            BotId = request.BotId,
            ChatId = request.ChatId,
            LeadId = request.LeadId,
            CampaignId = request.CampaignId,
            CampaignName = request.CampaignName,
            SourceId = request.SourceId,
            SourceName = request.SourceName,

            FunnelId = funnel?.Id,
            FunnelName = funnel?.Name,

            FlowId = flow?.Id,
            FlowName = flow?.Name,

            NodeId = node?.Id,
            NodeLabel = node?.Data?.Label,

            Status = node?.Data is AiReplyNodeData ? LeadFunnelStatus.Waiting : (node?.Data?.FinishStatus ?? LeadFunnelStatus.Manual),

            IsInputTranslatorOn = funnel?.IsInputTranslatorOn ?? false,
            IsOutputTranslatorOn = funnel?.IsOutputTranslatorOn ?? false,

            PhotoRecognition = funnel?.PhotoRecognition ?? RecognitionType.Skip,
            VideoRecognition = funnel?.VideoRecognition ?? RecognitionType.Skip,
            VoiceRecognition = funnel?.VoiceRecognition ?? RecognitionType.Skip,

            StatesLog = []
        };

        List<ActionTaskDocument> actionTasks = [];

        if (node != null)
        {
            actionTasks = CreateActionTasks(leadState, node);

            var now = DateTime.UtcNow;

            var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
            {
                ActionId = t.ActionId,
                Type = t.Type,
                Status = ActionStatus.Pending,
                StatusChangedAt = now
            }).ToList();

            leadState.StatesLog =
            [
                new StateLogEntry
            {
                NodeId = node.Id,
                EnteredAt = now,
                ActionsLog = actionStatusEntries
            }
            ];
        }

        try
        {
            await leadStateRepository.CreateLeadState(leadState, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            logger.LogWarning(
                "Lead {LeadId} already exists in funnel {FunnelId}, skipping entry",
                request.LeadId,
                request.FunnelId);

            return;
        }

        if (actionTasks.Count > 0)
        {
            await actionTaskRepository.CreateMany(actionTasks, ct);
        }

        logger.LogInformation(
            "Lead {LeadStateId} entered funnel {FunnelId} at node {NodeId}",
            leadState.Id,
            leadState.FunnelId,
            leadState.NodeId);

        if (node != null &&
            actionTasks.Count == 0 &&
            node.Data?.FinishStatus != LeadFunnelStatus.Waiting)
        {
            await TransitionToNextNode(leadState.Id, ct);
        }
    }
    public async Task TransitionToNextNode(Guid leadStateId, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadState(leadStateId, ct);

        if (leadState.FunnelId == null)
            throw new InvalidOperationException($"Funnel id id null");

        var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value)
            ?? throw new InvalidOperationException($"Funnel '{leadState.FunnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == leadState.FlowId)
            ?? throw new InvalidOperationException($"Flow '{leadState.FlowId}' not found.");

        var currentNode = flow.Nodes.FirstOrDefault(n => n.Id == leadState.NodeId);
        if (currentNode?.Data is ChangeFlowNodeData changeFlow)
        {
            var targetFlow = funnel.Flows.FirstOrDefault(f => f.Id == changeFlow.FlowId)
                ?? throw new InvalidOperationException($"Flow '{changeFlow.FlowId}' not found in funnel '{funnel.Id}'.");

            var targetNode = targetFlow.Nodes.FirstOrDefault(n => n.Id == changeFlow.NodeId)
                ?? throw new InvalidOperationException($"Node '{changeFlow.NodeId}' not found in flow '{targetFlow.Id}'.");

            leadState.FlowId = targetFlow.Id;

            await ExecuteTransition(leadStateId, leadState, funnel, targetFlow,
                new PassEdge(Id: Guid.Empty, Source: currentNode.Id, Target: targetNode.Id), ct);
            return;
        }

        var outgoingEdges = flow.Edges
            .Where(e => e.Source == leadState.NodeId)
            .ToList();

        if (outgoingEdges.Count == 0)
        {
            logger.LogInformation("Lead {LeadStateId} reached end of funnel {FunnelId}",
                leadStateId, leadState.FunnelId);

            //leadState.Status = LeadFunnelStatus.Finished;
            await leadStateRepository.UpdateLeadStateStatus(leadStateId, LeadFunnelStatus.Finished, ct);

            return;
        }

        var selectedEdge = await edgeRouter.SelectEdge(leadStateId, outgoingEdges, ct);
        if (selectedEdge is null)
        {
            logger.LogWarning("No edge selected for lead {LeadStateId} at node {NodeId}",
                leadStateId, leadState.NodeId);
            return;
        }

        await ExecuteTransition(leadStateId, leadState, funnel, flow, selectedEdge, ct);
    }

    public async Task ExecuteTransitionByEdge(Guid leadStateId, Guid edgeId, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadState(leadStateId, ct);

        if (leadState.FunnelId == null)
            throw new InvalidOperationException("Funnel id is null");

        var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value)
            ?? throw new InvalidOperationException($"Funnel '{leadState.FunnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == leadState.FlowId)
            ?? throw new InvalidOperationException($"Flow '{leadState.FlowId}' not found.");

        var edge = flow.Edges.FirstOrDefault(e => e.Id == edgeId)
            ?? throw new InvalidOperationException($"Edge '{edgeId}' not found in flow.");

        await ExecuteTransition(leadStateId, leadState, funnel, flow, edge, ct);
    }

    private async Task ExecuteTransition(
        Guid leadStateId,
        FunnelLeadState leadState,
        Funnel funnel,
        Flow flow,
        Edge selectedEdge,
        CancellationToken ct)
    {
        var targetNode = flow.Nodes.FirstOrDefault(n => n.Id == selectedEdge.Target);
        if (targetNode == null)
        {
            await leadStateRepository.UpdateLeadStateStatus(leadStateId, LeadFunnelStatus.Manual, ct);
            throw new InvalidOperationException($"Target node '{selectedEdge.Target}' not found.");
        }

        await actionTaskRepository.CancelPendingByLead(leadStateId, ct);

        var actionTasks = CreateActionTasks(leadState, targetNode);

        var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
        {
            ActionId = t.ActionId,
            Type = t.Type,
            Status = ActionStatus.Pending,
            StatusChangedAt = DateTime.UtcNow
        }).ToList();

        //await leadStateRepository.UpdateLeadStateStatus(leadStateId, targetNode.Data.FinishStatus, ct);
        //await leadStateRepository.MoveToNode(leadStateId, selectedEdge.Id, targetNode.Id, actionStatusEntries, ct);

        var isAiReply = targetNode.Data is AiReplyNodeData;
        var nodeStatus = isAiReply ? LeadFunnelStatus.Waiting : targetNode.Data.FinishStatus;

        await leadStateRepository.SetLeadFunnelPosition(
                leadStateId: leadStateId,
                funnelId: funnel.Id,
                funnelName: funnel.Name,
                flowId: flow.Id,
                flowName: flow.Name,
                nodeId: targetNode.Id,
                nodeLabel: targetNode.Data.Label,
                status: nodeStatus,
                actionStatusEntries,
                exitEdgeId: selectedEdge.Id,
                ct
            );

        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} transitioned to node {NodeId} via edge {EdgeId}",
            leadStateId, targetNode.Id, selectedEdge.Id);

        if (!isAiReply && actionTasks.Count == 0 && nodeStatus != LeadFunnelStatus.Waiting)
            await TransitionToNextNode(leadStateId, ct);
    }

    public async Task SetLeadFunnelPosition(
        Guid tenantId,
        string leadId,
        Guid funnelId,
        Guid flowId,
        Guid nodeId,
        CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadStateByLeadId(tenantId, leadId, ct)
            ?? throw new KeyNotFoundException($"Lead state for lead '{leadId}' not found.");

        var funnel = funnelCache.GetFunnel(funnelId)
            ?? throw new InvalidOperationException($"Funnel '{funnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == flowId)
            ?? throw new InvalidOperationException($"Flow '{flowId}' not found in funnel '{funnel.Id}'.");

        var node = flow.Nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new InvalidOperationException($"Node '{nodeId}' not found in flow '{flow.Id}'.");

        await actionTaskRepository.CancelPendingByLead(leadState.Id, ct);

        leadState.FunnelId = funnelId;
        leadState.FlowId = flowId;
        leadState.NodeId = nodeId;

        var actionTasks = CreateActionTasks(leadState, node);

        var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
        {
            ActionId = t.ActionId,
            Type = t.Type,
            Status = ActionStatus.Pending,
            StatusChangedAt = DateTime.UtcNow
        }).ToList();

        await leadStateRepository.SetLeadFunnelPosition(
            leadState.Id,
            funnelId,
            funnel.Name,
            flowId,
            flow.Name,
            nodeId,
            node.Data.Label,
            node.Data.FinishStatus,
            actionStatusEntries,
            exitEdgeId: null,
            ct);

        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} manually moved to flow {FlowId} node {NodeId}",
            leadState.Id, flowId, nodeId);

        if (actionTasks.Count == 0 && node.Data.FinishStatus != LeadFunnelStatus.Waiting)
            await TransitionToNextNode(leadState.Id, ct);
    }

    #region by chat
    public async Task SetLeadStatus(
        Guid tenantId,
        Guid chatId,
        LeadFunnelStatus status,
        CancellationToken ct)
    {
        //var leadState = await leadStateRepository.GetLeadStateByLeadId(tenantId, leadId, ct)
        //    ?? throw new KeyNotFoundException($"Lead state for lead '{leadId}' not found.");

        await leadStateRepository.UpdateLeadStateStatusByChatId(chatId, status, ct);

        logger.LogInformation("Lead state chatId={LeadStateId} status manually set to {Status}", chatId, status);
    }

    public async Task UpdateLeadStateByChatId(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        SetLeadStateRequest dto,
        CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadStateByChatId(tenantId, chatId, ct);
            //?? throw new KeyNotFoundException($"Lead state for chat '{chatId}' not found.");

        if (leadState == null)
        {
            leadState = new FunnelLeadState
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                SpaceId = spaceId,
                BotId = botId,
                ChatId = chatId,
                LeadId = Nanoid.Generate(Nanoid.Alphabets.LettersAndDigits, size: 12),
                CampaignId = null,
                CampaignName = null,
                SourceId = null,
                SourceName = null,

                FunnelId = null,
                FunnelName = null,

                FlowId = null,
                FlowName = null,

                NodeId = null,
                NodeLabel = null,

                Status = LeadFunnelStatus.Manual,

                IsInputTranslatorOn = false,
                IsOutputTranslatorOn = false,

                StatesLog = []
            };

            await leadStateRepository.CreateLeadState(leadState, ct);
        }

        var positionFieldsSet = new[] { dto.FunnelId.HasValue, dto.FlowId.HasValue, dto.NodeId.HasValue };
        if (positionFieldsSet.Any(x => x) && !positionFieldsSet.All(x => x))
            throw new ArgumentException("FunnelId, FlowId and NodeId must either all be set or all be null.");

        if (dto.FunnelId.HasValue && dto.FlowId.HasValue && dto.NodeId.HasValue)
        {
            await SetLeadFunnelPosition(
                leadState.TenantId,
                leadState.LeadId,
                dto.FunnelId.Value,
                dto.FlowId.Value,
                dto.NodeId.Value,
                ct);
        }

        if (dto.Status.HasValue)
        {
            await leadStateRepository.UpdateLeadStateStatus(leadState.Id, dto.Status.Value, ct);
            logger.LogInformation("Lead {LeadStateId} status manually set to {Status}", leadState.Id, dto.Status.Value);
        }

        if (dto.Tags is not null)
        {
            await leadStateRepository.SaveTags(leadState.Id, dto.Tags, ct);
            logger.LogInformation("Lead {LeadStateId} tags set to [{Tags}]", leadState.Id, string.Join(", ", dto.Tags));
        }

        if (dto.IsInputTranslatorOn.HasValue || dto.IsOutputTranslatorOn.HasValue)
        {
            await leadStateRepository.SetIsTranslatorOn(leadState.Id, dto.IsInputTranslatorOn, dto.IsOutputTranslatorOn, ct);
            logger.LogInformation("Lead {LeadStateId} translator set to input={IsInputTranslatorOn}, output={IsOutputTranslatorOn}", leadState.Id, dto.IsInputTranslatorOn, dto.IsOutputTranslatorOn);
        }
    }
    public async Task HandleIncomingSignal(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {

        // Атомарно захватываем лид: переводим Waiting → Nothing только если статус ещё Waiting.
        // Если другой воркер уже захватил — вернётся null, и мы просто выходим.
        var leadState = await leadStateRepository.ClaimWaitingLeadByChatId(tenantId, botId, chatId, ct);
        if (leadState is null) return;


        if (leadState.NodeId.HasValue && leadState.FunnelId.HasValue)
        {
            var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value);
            var flow = funnel?.Flows.FirstOrDefault(f => f.Id == leadState.FlowId);
            var currentNode = flow?.Nodes.FirstOrDefault(n => n.Id == leadState.NodeId);

            if (currentNode?.Data is AiReplyNodeData && flow is not null)
            {
                var triggeredEdgeIds = GetTriggeredEdgeIds(leadState);
                var aiRouterEdges = flow.Edges
                    .Where(e => e.Source == leadState.NodeId)
                    .OfType<AiRouterEdge>()
                    .Where(e => !e.TriggerOnce || !triggeredEdgeIds.Contains(e.Id))
                    .ToList();

                if (aiRouterEdges.Count > 0)
                {
                    logger.LogInformation($"AiRouter: Task enqueued (currentNode={currentNode.Data.Label})");

                    var task = new AiRouterActionTaskDocument
                    {
                        Id = Guid.CreateVersion7(),
                        TenantId = leadState.TenantId,
                        SpaceId = leadState.SpaceId,
                        LeadStateId = leadState.Id,
                        FunnelId = leadState.FunnelId.Value,
                        FlowId = leadState.FlowId!.Value,
                        NodeId = leadState.NodeId.Value,
                        ActionId = Guid.CreateVersion7(),
                        BotId = leadState.BotId,
                        ChatId = leadState.ChatId,
                        ScheduledAt = DateTime.UtcNow + TimeSpan.FromSeconds(5),
                        CreatedAt = DateTime.UtcNow,
                        Order = 0
                    };

                    await actionTaskRepository.UpsertPendingAiRouterTask(task, ct);
                    await leadStateRepository.UpdateLeadStateStatus(leadState.Id, LeadFunnelStatus.Waiting, ct);
                    return;
                }

                var hasPassOrSplit = flow.Edges.Any(e => e.Source == leadState.NodeId && e is PassEdge or SplitEdge);
                if (hasPassOrSplit)
                {
                    var replyTask = new AiReplyActionTaskDocument
                    {
                        Id = Guid.CreateVersion7(),
                        TenantId = leadState.TenantId,
                        SpaceId = leadState.SpaceId,
                        LeadStateId = leadState.Id,
                        FunnelId = leadState.FunnelId.Value,
                        FlowId = leadState.FlowId!.Value,
                        NodeId = leadState.NodeId.Value,
                        ActionId = Guid.CreateVersion7(),
                        BotId = leadState.BotId,
                        ChatId = leadState.ChatId,
                        ScheduledAt = DateTime.UtcNow + TimeSpan.FromSeconds(funnel!.ReplyDelay),
                        CreatedAt = DateTime.UtcNow,
                        Order = 0,
                        TransitionAfterReply = true
                    };

                    await actionTaskRepository.TryInsertAiReplyTask(replyTask, ct);
                    await leadStateRepository.UpdateLeadStateStatus(leadState.Id, LeadFunnelStatus.Waiting, ct);
                    return;
                }
            }
        }

        // Проверяем завершённость actions прямо по уже полученному документу — без доп. запроса.
        var currentLog = leadState.StatesLog.LastOrDefault(s => s.NodeId == leadState.NodeId && s.LeftAt == null);
        bool allDone = currentLog is null
            || currentLog.ActionsLog.Count == 0
            || currentLog.ActionsLog.All(a => a.Status == ActionStatus.Completed);

        if (!allDone)
        {
            // Actions ещё не завершены — возвращаем статус Waiting, сигнал проигнорируем.
            await leadStateRepository.UpdateLeadStateStatus(leadState.Id, LeadFunnelStatus.Waiting, ct);
            return;
        }      

        await TransitionToNextNode(leadState.Id, ct);
    }

    public async Task ClearLeadStateByChat(Guid tenantId, Guid chatId)
    {
        var leadState = await leadStateRepository.GetLeadStateByChatId(tenantId, chatId, CancellationToken.None);
        if (leadState is null)
            return;

        await actionTaskRepository.CancelPendingByLead(leadState.Id, CancellationToken.None);
        await leadStateRepository.Delete(leadState.Id, CancellationToken.None);

        logger.LogInformation("Lead state {LeadStateId} cleared for chat {ChatId}", leadState.Id, chatId);
    }
    #endregion

    #endregion
}
