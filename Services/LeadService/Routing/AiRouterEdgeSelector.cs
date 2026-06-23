using states.Dtos.Edges;
using states.Dtos.Funnels;
using states.Mongo.Documents;

namespace states.Services.LeadService.Routing;

public static class AiRouterEdgeSelector
{
    public static List<AiRouterEdge> GetEligibleEdges(Flow flow, Guid nodeId, FunnelLeadState leadState)
    {
        var triggeredEdgeIds = leadState.StatesLog
            .Where(s => s.ExitEdgeId.HasValue)
            .Select(s => s.ExitEdgeId!.Value)
            .ToHashSet();

        var nodeIds = flow.Nodes.Select(n => n.Id).ToHashSet();

        return flow.Edges
            .Where(e => e.Source == nodeId)
            .OfType<AiRouterEdge>()
            .Where(e => !e.TriggerOnce || !triggeredEdgeIds.Contains(e.Id))
            .Where(e => !string.IsNullOrWhiteSpace(e.Thesis))
            .Where(e => nodeIds.Contains(e.Target))
            .ToList();
    }
}
