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
    private readonly IPushTaskRepository pushTaskRepository;
    private readonly IFunnelRuntimeCache funnelCache;
    private readonly IEdgeRouter edgeRouter;
    private readonly ILogger<LeadProgressionService> logger;

    public LeadProgressionService(
        ILeadStateRepository leadStateRepository,
        IActionTaskRepository actionTaskRepository,
        IPushTaskRepository pushTaskRepository,
        IFunnelRuntimeCache funnelCache,
        IEdgeRouter edgeRouter,
        ILogger<LeadProgressionService> logger)
    {
        this.leadStateRepository = leadStateRepository;
        this.actionTaskRepository = actionTaskRepository;
        this.pushTaskRepository = pushTaskRepository;
        this.funnelCache = funnelCache;
        this.edgeRouter = edgeRouter;
        this.logger = logger;
    }

    #region private
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

                case AiReplyNodeData:

                var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value);

                if (funnel is not null)
                {
                    var flow = funnel.Flows.FirstOrDefault(f => f.Id == leadState.FlowId);
                    var isRevisit = leadState.StatesLog.Any(s => s.NodeId == node.Id);

                    var aiRouterEdges = isRevisit && flow is not null
                        ? AiRouterEdgeSelector.GetEligibleEdges(flow, node.Id, leadState)
                        : [];

                    if (aiRouterEdges.Count > 0)
                    {
                        // Лид уже был на этой AiReply-ноде раньше — значит контекст для роутинга уже есть,
                        // сначала пробуем роутер и только при отсутствии матча генерируем новый ответ.
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
                            CreatedAt = now,

                            BotId = leadState.BotId,
                            ChatId = leadState.ChatId,
                            ScheduledAt = now
                        });
                    }
                    else
                    {
                        var hasPassOrSplit = isRevisit && flow is not null &&
                            flow.Edges.Any(e => e.Source == node.Id && e is PassEdge or SplitEdge);

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

                            TransitionAfterReply = hasPassOrSplit
                        });
                    }
                }
                break;

            case SendWebhookNodeData sendWebhook:
                tasks.Add(new SendWebhookActionTaskDocument
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
                    ScheduledAt = now,
                    CreatedAt = now,

                    Url = sendWebhook.Url,
                    MethodType = sendWebhook.MethodType
                });
                break;
        }

        return tasks;
    }

    // Пуши настраиваются на SendPreset- и AiReply-нодах, лежат в отдельной коллекции push_tasks
    // со своей цепочкой Order и не участвуют в ActionsLog/переходе по воронке.
    private List<PushTaskDocument> CreatePushTasks(FunnelLeadState leadState, Dtos.Nodes.Node node)
    {
        if (leadState.FunnelId is null || leadState.FlowId is null)
            return [];

        var nodePushes = node.Data switch
        {
            SendPresetNodeData sendPreset => sendPreset.Pushes,
            AiReplyNodeData aiReply => aiReply.Pushes,
            _ => []
        };

        if (nodePushes.Count == 0)
            return [];

        // Уже успешно отправленные пуши (см. FunnelLeadState.Pushes) не пересоздаём при повторном
        // входе в ноду — иначе при каждом revisit лид получал бы один и тот же пуш заново.
        var pushes = nodePushes.Where(p => !leadState.Pushes.Contains(p.Id)).ToList();
        if (pushes.Count == 0)
            return [];

        var now = DateTime.UtcNow;

        // Расписание считаем от первого входа в ноду, а не от текущего возвращения — иначе повторный
        // переход в ту же ноду сдвигал бы тайминг оставшихся пушей вперёд на каждый revisit.
        var firstEnteredAt = leadState.StatesLog
            .Where(s => s.NodeId == node.Id)
            .Select(s => s.EnteredAt)
            .DefaultIfEmpty(now)
            .Min();

        // Накопление задержки считаем по полному конфигу пушей ноды (включая уже отправленные),
        // чтобы delay каждого пуша остался отсчитан от предыдущего так же, как при первом входе.
        var scheduleByPushId = new Dictionary<Guid, DateTime>();
        var cumulative = firstEnteredAt;
        foreach (var p in nodePushes)
        {
            if (p.Delay.HasValue)
                cumulative += p.Delay.Value;
            scheduleByPushId[p.Id] = cumulative;
        }

        var pushTasks = new List<PushTaskDocument>();

        for (var i = 0; i < pushes.Count; i++)
        {
            var push = pushes[i];

            pushTasks.Add(new PushTaskDocument
            {
                Id = Guid.CreateVersion7(),
                TenantId = leadState.TenantId,
                SpaceId = leadState.SpaceId,
                LeadStateId = leadState.Id,
                FunnelId = leadState.FunnelId.Value,
                FlowId = leadState.FlowId.Value,
                NodeId = node.Id,
                PushId = push.Id,
                Order = i,
                Status = i == 0 ? ActionStatus.Pending : ActionStatus.Waiting,
                ScheduledAt = scheduleByPushId[push.Id],
                CreatedAt = now,

                BotId = leadState.BotId,
                ChatId = leadState.ChatId,
                PresetId = push.PresetId
            });
        }

        return pushTasks;
    }

    // Push-таски лежат в отдельной коллекции (своя цепочка Order, свой воркер) и не влияют
    // на ActionsLog/переход по воронке — отмена pending тасков лида затрагивает обе коллекции.
    private async Task CancelPendingTasks(Guid leadStateId, CancellationToken ct)
    {
        await actionTaskRepository.CancelPendingByLead(leadStateId, ct);
        await pushTaskRepository.CancelPendingByLead(leadStateId, ct);
    }

    // Action tasks текущей ноды были отменены при блокировке лида (см. MarkLeadBlocked) —
    // после разблокировки пересоздаём их так же, как при первом входе в ноду.
    private async Task RearmCurrentNodeActions(FunnelLeadState leadState, CancellationToken ct)
    {
        if (leadState.FunnelId is null || leadState.FlowId is null || leadState.NodeId is null)
            return;

        var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value);
        var flow = funnel?.Flows.FirstOrDefault(f => f.Id == leadState.FlowId);
        var node = flow?.Nodes.FirstOrDefault(n => n.Id == leadState.NodeId);
        if (node is null)
            return;

        // AiReply-ноды сами пересоздают себе AiRouter/AiReply задачу на каждый входящий сигнал
        // (см. блок ниже в HandleIncomingSignal) — рармить их тут не нужно, иначе статус лишний
        // раз дёргается Waiting → Nothing → Waiting на одном и том же сообщении.
        if (node.Data is AiReplyNodeData)
            return;

        var actionTasks = CreateActionTasks(leadState, node);
        var pushTasks = CreatePushTasks(leadState, node);
        if (actionTasks.Count == 0 && pushTasks.Count == 0)
            return;

        var now = DateTime.UtcNow;

        if (actionTasks.Count > 0)
        {
            var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
            {
                ActionId = t.ActionId,
                Type = t.Type,
                Status = ActionStatus.Pending,
                StatusChangedAt = now
            }).ToList();

            await leadStateRepository.ResetCurrentNodeActions(leadState.Id, actionStatusEntries, ct);
            await actionTaskRepository.CreateMany(actionTasks, ct);
        }

        if (pushTasks.Count > 0)
            await pushTaskRepository.CreateMany(pushTasks, ct);

        logger.LogInformation("Lead {LeadStateId} re-armed {ActionCount} action task(s) and {PushCount} push task(s) for node {NodeId} after unblock",
            leadState.Id, actionTasks.Count, pushTasks.Count, node.Id);
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

            StatesLog = [],

            StartParameter = request.StartParameter
        };

        List<ActionTaskDocument> actionTasks = [];
        List<PushTaskDocument> pushTasks = [];

        if (node != null)
        {
            actionTasks = CreateActionTasks(leadState, node);
            pushTasks = CreatePushTasks(leadState, node);

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

        if (pushTasks.Count > 0)
        {
            await pushTaskRepository.CreateMany(pushTasks, ct);
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

        await CancelPendingTasks(leadStateId, ct);

        var actionTasks = CreateActionTasks(leadState, targetNode);
        var pushTasks = CreatePushTasks(leadState, targetNode);

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
        await pushTaskRepository.CreateMany(pushTasks, ct);

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
        var leadStates = await leadStateRepository.GetLeadStatesByLeadId(tenantId, leadId, ct);
        if (leadStates.Count == 0)
            throw new KeyNotFoundException($"Lead state for lead '{leadId}' not found.");

        if (leadStates.Count > 1)
        {
            // Кампания может вести лида через несколько ботов. Перемещаем только одно
            // состояние лида — остальные не переместятся.
            logger.LogWarning(
                "Lead '{LeadId}' for tenant {TenantId} matched {Count} lead states, processing only one",
                leadId, tenantId, leadStates.Count);
        }

        // Уникальный индекс (tenantId, funnelId, leadId) допускает только один стейт лида в воронке.
        // Если один из стейтов уже находится в целевой воронке — двигать можно только его, попытка
        // перевести туда любой другой стейт упадёт с E11000 DuplicateKey. Иначе берём самое первое
        // состояние (создано при входе лида в воронку) — репозиторий возвращает список
        // отсортированным по createdAt, так что [0] — именно оно.
        var leadState = leadStates.FirstOrDefault(s => s.FunnelId == funnelId) ?? leadStates[0];

        var funnel = funnelCache.GetFunnel(funnelId)
            ?? throw new InvalidOperationException($"Funnel '{funnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == flowId)
            ?? throw new InvalidOperationException($"Flow '{flowId}' not found in funnel '{funnel.Id}'.");

        var node = flow.Nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new InvalidOperationException($"Node '{nodeId}' not found in flow '{flow.Id}'.");

        await CancelPendingTasks(leadState.Id, ct);

        leadState.FunnelId = funnelId;
        leadState.FlowId = flowId;
        leadState.NodeId = nodeId;

        var actionTasks = CreateActionTasks(leadState, node);
        var pushTasks = CreatePushTasks(leadState, node);

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
        await pushTaskRepository.CreateMany(pushTasks, ct);

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

    public async Task MarkLeadBlocked(Guid tenantId, Guid chatId, CancellationToken ct)
    {
        var updated = await leadStateRepository.MarkBlockedByChatId(chatId, ct);
        if (updated is null)
            return;

        await CancelPendingTasks(updated.Id, ct);

        logger.LogInformation("Lead state chatId={ChatId} blocked, preBlockStatus={PreBlockStatus}", chatId, updated.PreBlockStatus);
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

    public async Task<int> BackfillDeposits(CancellationToken ct)
    {
        var leads = await leadStateRepository.GetLeadIdsWithDeposits(ct);
        logger.LogInformation("Deposit backfill started: {Count} leads with deposits", leads.Count);

        var processed = 0;
        foreach (var (tenantId, leadId) in leads)
        {
            ct.ThrowIfCancellationRequested();

            // Тот же путь, что при обычном депозите: пересчёт из leadEvents + веер outbox-событий
            // на все состояния лида. OutboxWorkerService уже разошлёт их в tgengine.
            await leadStateRepository.RecalculateDeposits(tenantId, leadId, ct);
            processed++;

            if (processed % 500 == 0)
                logger.LogInformation("Deposit backfill progress: {Processed}/{Total}", processed, leads.Count);
        }

        logger.LogInformation("Deposit backfill finished: {Processed} leads processed", processed);
        return processed;
    }
    public async Task HandleIncomingSignal(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {

        // Атомарно захватываем лид: переводим Waiting → Nothing только если статус ещё Waiting.
        // Если другой воркер уже захватил — вернётся null, и мы просто выходим.
        var leadState = await leadStateRepository.ClaimWaitingLeadByChatId(tenantId, botId, chatId, ct);

        if (leadState is null)
        {
            // Не Waiting — возможно лид был заблокирован, а это сообщение значит, что он разблокировал бота.
            // UnblockByChatId сам не делает ничего, если лид не Blocked — лишней записи в общем случае нет.
            var unblocked = await leadStateRepository.UnblockByChatId(tenantId, botId, chatId, ct);
            if (unblocked is null) return;

            await RearmCurrentNodeActions(unblocked, ct);

            leadState = await leadStateRepository.ClaimWaitingLeadByChatId(tenantId, botId, chatId, ct);
            if (leadState is null) return;
        }


        if (leadState.NodeId.HasValue && leadState.FunnelId.HasValue)
        {
            var funnel = funnelCache.GetFunnel(leadState.FunnelId.Value);
            var flow = funnel?.Flows.FirstOrDefault(f => f.Id == leadState.FlowId);
            var currentNode = flow?.Nodes.FirstOrDefault(n => n.Id == leadState.NodeId);

            if (funnel is not null && leadState.FlowId.HasValue)
            {
                // Прочитку ставим отдельным таском с задержкой ReadDelay — лид видит «прочитано»
                // через несколько секунд после своего сообщения, а ответ приходит позже (натурально).
                // Ставим на любой входящий сигнал, не только на AiReply-нодах: если сообщение
                // триггерит переход в новую ноду, прочитка всё равно должна отработать
                // (MarkRead переживает отмену тасков при переходе — см. CancelPendingByLead).
                var readTask = new MarkReadActionTaskDocument
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = leadState.TenantId,
                    SpaceId = leadState.SpaceId,
                    LeadStateId = leadState.Id,
                    FunnelId = leadState.FunnelId.Value,
                    FlowId = leadState.FlowId.Value,
                    NodeId = leadState.NodeId.Value,
                    ActionId = Guid.CreateVersion7(),
                    BotId = leadState.BotId,
                    ChatId = leadState.ChatId,
                    ScheduledAt = DateTime.UtcNow + TimeSpan.FromSeconds(funnel.ReadDelay),
                    CreatedAt = DateTime.UtcNow,
                    Order = 0
                };

                await actionTaskRepository.UpsertPendingMarkReadTask(readTask, ct);
            }

            if (currentNode?.Data is AiReplyNodeData && flow is not null)
            {
                var aiRouterEdges = AiRouterEdgeSelector.GetEligibleEdges(flow, leadState.NodeId.Value, leadState);

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
                    await leadStateRepository.TrySetWaitingIfStillOnNode(leadState.Id, leadState.NodeId.Value, ct);
                    return;
                }

                // Нет ни одного исходящего AiRouter-edge. Если есть обычный Pass/Split — отвечаем и
                // переходим дальше по флоу; если исходящих edges нет вообще — это терминальная
                // AI-нода, лид остаётся на ней и продолжает общаться с ИИ (не Finished).
                var hasPassOrSplit = flow.Edges.Any(e => e.Source == leadState.NodeId && e is PassEdge or SplitEdge);

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
                    // ReplyDelay отсчитывается после прочитки: сначала «прочитано», потом пауза на ответ
                    ScheduledAt = DateTime.UtcNow + TimeSpan.FromSeconds(funnel!.ReadDelay + funnel.ReplyDelay),
                    CreatedAt = DateTime.UtcNow,
                    Order = 0,
                    TransitionAfterReply = hasPassOrSplit,
                    ReplyOnlyIfLastIncoming = true
                };

                await actionTaskRepository.TryInsertAiReplyTask(replyTask, ct);
                await leadStateRepository.TrySetWaitingIfStillOnNode(leadState.Id, leadState.NodeId.Value, ct);
                return;
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
            // currentLog здесь не null (иначе allDone == true), так что NodeId ноды известен.
            await leadStateRepository.TrySetWaitingIfStillOnNode(leadState.Id, currentLog!.NodeId, ct);
            return;
        }

        await TransitionToNextNode(leadState.Id, ct);
    }

    public async Task ClearLeadStateByChat(Guid tenantId, Guid chatId)
    {
        var leadState = await leadStateRepository.GetLeadStateByChatId(tenantId, chatId, CancellationToken.None);
        if (leadState is null)
            return;

        await CancelPendingTasks(leadState.Id, CancellationToken.None);
        await leadStateRepository.Delete(leadState.Id, CancellationToken.None);

        logger.LogInformation("Lead state {LeadStateId} cleared for chat {ChatId}", leadState.Id, chatId);
    }
    #endregion

    #endregion
}
