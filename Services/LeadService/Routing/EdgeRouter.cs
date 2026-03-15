using states.Dtos.Edges;

namespace states.Services.LeadService.Routing;

public class EdgeRouter : IEdgeRouter
{
    private static readonly Random random = new();
    private readonly IAiRouterClient aiClient;

    public EdgeRouter(IAiRouterClient aiClient)
    {
        this.aiClient = aiClient;
    }

    public async Task<Edge?> SelectEdge(Guid leadStateId, IReadOnlyList<Edge> edges, CancellationToken ct)
    {
        if (edges.Count == 0)
            return null;

        return edges[0] switch
        {
            PassEdge => edges[0],
            SplitEdge => SelectSplit(edges),
            AiRouterEdge => await SelectAiRouter(leadStateId, edges, ct),
            _ => null
        };
    }

    private static Edge? SelectSplit(IReadOnlyList<Edge> edges)
    {
        var splits = edges.OfType<SplitEdge>().ToList();
        if (splits.Count == 0) return null;

        var totalPercentage = splits.Sum(e => e.Percentage);
        if (totalPercentage == 0) return null;

        var roll = random.Next(1, totalPercentage + 1);
        var cumulative = 0;

        foreach (var split in splits)
        {
            cumulative += split.Percentage;
            if (roll <= cumulative)
                return split;
        }

        return splits.Last();
    }

    private async Task<Edge?> SelectAiRouter(Guid leadStateId, IReadOnlyList<Edge> edges, CancellationToken ct)
    {
        foreach (var edge in edges.OfType<AiRouterEdge>())
        {
            var match = await aiClient.CheckThesis(leadStateId, edge.Thesis, ct);
            if (match)
                return edge;
        }

        return null;
    }
}
