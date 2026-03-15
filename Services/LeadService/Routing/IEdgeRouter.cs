using states.Dtos.Edges;

namespace states.Services.LeadService.Routing;

public interface IEdgeRouter
{
    Task<Edge?> SelectEdge(Guid leadStateId, IReadOnlyList<Edge> edges, CancellationToken ct);
}
