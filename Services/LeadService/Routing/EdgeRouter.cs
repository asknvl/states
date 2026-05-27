using states.Dtos.Edges;

namespace states.Services.LeadService.Routing;

public class EdgeRouter : IEdgeRouter
{
    private static readonly Random random = new();

    public async Task<Edge?> SelectEdge(Guid leadStateId, IReadOnlyList<Edge> edges, CancellationToken ct)
    {
        if (edges.Count == 0)
            return null;

        return edges[0] switch
        {
            PassEdge => edges[0],
            SplitEdge => SelectSplit(edges),            
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

}
