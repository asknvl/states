using states.Dtos.Edges;
using states.Dtos.Nodes;

namespace states.Dtos.Funnels
{
    public sealed record Flow(
            Guid Id,
            string Name,
            List<Node> Nodes,
            List<Edge> Edges
        );
}
