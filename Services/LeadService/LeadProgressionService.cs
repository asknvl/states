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
            BotId = request.BotId,
            ChatId = request.ChatId,
            FunnelId = request.FunnelId,
            FlowId = request.FlowId,
            NodeId = request.NodeId
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

        await leadStateRepository.Create(leadState, ct);
        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} entered funnel {FunnelId} at node {NodeId}",
            leadState.Id, funnel.Id, node.Id);

        if (actionTasks.Count == 0)
            await TransitionToNextNode(leadState.Id, ct);
    }

    public async Task TransitionToNextNode(Guid leadStateId, CancellationToken ct)
    {
        var leadState = await leadStateRepository.Get(leadStateId, ct);

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
            return;
        }

        var selectedEdge = await edgeRouter.SelectEdge(leadStateId, outgoingEdges, ct);
        if (selectedEdge is null)
        {
            logger.LogWarning("No edge selected for lead {LeadStateId} at node {NodeId}",
                leadStateId, leadState.NodeId);
            return;
        }

        var targetNode = flow.Nodes.FirstOrDefault(n => n.Id == selectedEdge.Target)
            ?? throw new InvalidOperationException($"Target node '{selectedEdge.Target}' not found.");

        var actionTasks = CreateActionTasks(leadState, targetNode);

        var actionStatusEntries = actionTasks.Select(t => new ActionStatusEntry
        {
            ActionId = t.ActionId,
            Type = t.Type,
            Status = ActionStatus.Pending,
            StatusChangedAt = DateTime.UtcNow
        }).ToList();

        await leadStateRepository.MoveToNode(leadStateId, selectedEdge.Id, targetNode.Id, actionStatusEntries, ct);
        await actionTaskRepository.CreateMany(actionTasks, ct);

        logger.LogInformation("Lead {LeadStateId} transitioned to node {NodeId} via edge {EdgeId}",
            leadStateId, targetNode.Id, selectedEdge.Id);

        if (actionTasks.Count == 0)
            await TransitionToNextNode(leadStateId, ct);
    }

    private List<ActionTaskDocument> CreateActionTasks(FunnelLeadState leadState, Node node)
    {
        var now = DateTime.UtcNow;
        var tasks = new List<ActionTaskDocument>();

        switch (node.Data)
        {
            case SendPresetNodeData sendPreset:
                foreach (var action in sendPreset.Actions)
                {
                    tasks.Add(new SendPresetActionTaskDocument
                    {
                        Id = Guid.CreateVersion7(),
                        LeadStateId = leadState.Id,
                        FunnelId = leadState.FunnelId,
                        FlowId = leadState.FlowId,
                        NodeId = node.Id,
                        ActionId = action.Id,
                        ScheduledAt = now + action.Delay,
                        CreatedAt = now,
                        PresetId = action.PresetId,
                        NeedPin = action.NeedPin
                    });
                }
                break;

            case ManageTagNodeData manageTag:
                tasks.Add(new ManageTagActionTaskDocument
                {
                    Id = Guid.CreateVersion7(),
                    LeadStateId = leadState.Id,
                    FunnelId = leadState.FunnelId,
                    FlowId = leadState.FlowId,
                    NodeId = node.Id,
                    ActionId = Guid.CreateVersion7(),
                    ScheduledAt = now,
                    CreatedAt = now,
                    Operation = manageTag.Operation,
                    TagId = manageTag.TagId,
                    ReplacementTagId = manageTag.ReplacementTagId
                });
                break;
        }

        return tasks;
    }
}
