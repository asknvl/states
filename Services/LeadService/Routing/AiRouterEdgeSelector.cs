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

        // Эдж, которым лид покинул эту же ноду в прошлый раз. Даже если TriggerOnce выключен и
        // условие в целом разрешено проверять повторно за диалог, нельзя выбирать тот же эдж два
        // раза подряд — иначе лид зацикливается между нодами без нового сообщения от пользователя.
        // Если между визитами лид уходил другим эджем, этот снова становится доступен.
        var lastExitEdgeFromNode = leadState.StatesLog
            .Where(s => s.NodeId == nodeId && s.ExitEdgeId.HasValue)
            .OrderByDescending(s => s.EnteredAt)
            .Select(s => s.ExitEdgeId)
            .FirstOrDefault();

        var nodeIds = flow.Nodes.Select(n => n.Id).ToHashSet();

        return flow.Edges
            .Where(e => e.Source == nodeId)
            .OfType<AiRouterEdge>()
            .Where(e => !e.TriggerOnce || !triggeredEdgeIds.Contains(e.Id))
            .Where(e => e.Id != lastExitEdgeFromNode)
            .Where(e => !string.IsNullOrWhiteSpace(e.Thesis))
            .Where(e => nodeIds.Contains(e.Target))
            .ToList();
    }
}
