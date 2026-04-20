using MongoDB.Driver;
using states.Dtos.Edges;
using states.Dtos.Leads;
using states.Dtos.Nodes;
using states.Mongo.Documents;
using states.Mongo.Repositories;
using states.Services.FunnelService.Application;
using states.Services.FunnelService.Runtime;
using states.Services.LeadService.Routing;

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
    private List<ActionTaskDocument> CreateActionTasks(FunnelLeadState leadState, Node node)
    {
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
                        FunnelId = leadState.FunnelId,
                        FlowId = leadState.FlowId,
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
                        FunnelId = leadState.FunnelId,
                        FlowId = leadState.FlowId,
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
        }

        return tasks;
    }
    #endregion

    #region public
    public async Task EnterFunnel(EnterFunnelRequest request, CancellationToken ct)
    {
        var funnel = funnelCache.GetFunnel(request.FunnelId)
            ?? throw new InvalidOperationException($"Funnel '{request.FunnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == request.FlowId)
            ?? throw new InvalidOperationException($"Flow '{request.FlowId}' not found in funnel '{funnel.Id}'.");

        var node = flow.Nodes.FirstOrDefault(n => n.Id == request.NodeId)
            ?? throw new InvalidOperationException($"Node '{request.NodeId}' not found in flow '{flow.Id}'.");

        var leadState = new FunnelLeadState
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            SpaceId = request.SpaceId,
            BotId = request.BotId,
            ChatId = request.ChatId,
            LeadId = request.LeadId,
            FunnelId = request.FunnelId,
            FlowId = request.FlowId,
            NodeId = request.NodeId,
            Status = node.Data.FinishStatus           
        };

        var actionTasks = CreateActionTasks(leadState, node);

        var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
        {
            ActionId = t.ActionId,
            Type = t.Type,
            Status = ActionStatus.Pending,
            StatusChangedAt = DateTime.UtcNow
        }).ToList();

        leadState.StatesLog =
        [
            new StateLogEntry
            {
                NodeId = node.Id,
                EnteredAt = DateTime.UtcNow,
                ActionsLog = actionStatusEntries
            }
        ];

        try
        {
            await leadStateRepository.CreateLeadState(leadState, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            logger.LogWarning("Lead {LeadId} already exists in funnel {FunnelId}, skipping entry",
                request.LeadId, request.FunnelId);
            return;
        }

        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} entered funnel {FunnelId} at node {NodeId}",
            leadState.Id, funnel.Id, node.Id);

        if (actionTasks.Count == 0)
        {
            if (node.Data.FinishStatus != LeadFunnelStatus.Waiting)
            {
                await TransitionToNextNode(leadState.Id, ct);
            }
        }
    }
    public async Task TransitionToNextNode(Guid leadStateId, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadState(leadStateId, ct);

        var funnel = funnelCache.GetFunnel(leadState.FunnelId)
            ?? throw new InvalidOperationException($"Funnel '{leadState.FunnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == leadState.FlowId)
            ?? throw new InvalidOperationException($"Flow '{leadState.FlowId}' not found.");

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

        //leadState.Status = targetNode.Data.FinishStatus;
        await leadStateRepository.UpdateLeadStateStatus(leadStateId, targetNode.Data.FinishStatus, ct);

        await leadStateRepository.MoveToNode(leadStateId, selectedEdge.Id, targetNode.Id, actionStatusEntries, ct);

        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} transitioned to node {NodeId} via edge {EdgeId}",
            leadStateId, targetNode.Id, selectedEdge.Id);

        if (actionTasks.Count == 0)
            await TransitionToNextNode(leadStateId, ct);
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

    public async Task SetLeadFlowAndNode(Guid tenantId, string leadId, Guid flowId, Guid nodeId, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadStateByLeadId(tenantId, leadId, ct)
            ?? throw new KeyNotFoundException($"Lead state for lead '{leadId}' not found.");

        var funnel = funnelCache.GetFunnel(leadState.FunnelId)
            ?? throw new InvalidOperationException($"Funnel '{leadState.FunnelId}' not found in cache.");

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == flowId)
            ?? throw new InvalidOperationException($"Flow '{flowId}' not found in funnel '{funnel.Id}'.");

        var node = flow.Nodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new InvalidOperationException($"Node '{nodeId}' not found in flow '{flow.Id}'.");

        await actionTaskRepository.CancelPendingByLead(leadState.Id, ct);

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

        await leadStateRepository.UpdateLeadStateStatus(leadState.Id, node.Data.FinishStatus, ct);
        await leadStateRepository.SetFlowAndNode(leadState.Id, flowId, nodeId, actionStatusEntries, ct);
        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} manually moved to flow {FlowId} node {NodeId}",
            leadState.Id, flowId, nodeId);

        if (actionTasks.Count == 0)
            await TransitionToNextNode(leadState.Id, ct);
    }

    public async Task SetLeadStatus(Guid tenantId, string leadId, LeadFunnelStatus status, CancellationToken ct)
    {
        var leadState = await leadStateRepository.GetLeadStateByLeadId(tenantId, leadId, ct)
            ?? throw new KeyNotFoundException($"Lead state for lead '{leadId}' not found.");

        await leadStateRepository.UpdateLeadStateStatus(leadState.Id, status, ct);

        logger.LogInformation("Lead {LeadStateId} status manually set to {Status}", leadState.Id, status);
    }
    #endregion
}
