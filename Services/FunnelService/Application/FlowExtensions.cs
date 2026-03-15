using states.Dtos.Edges;
using states.Dtos.Funnels;
using states.Dtos.Nodes;

namespace states.Services.FunnelService.Runtime.Extensions
{
    public static class FlowExtensions
    {
        public static Node? GetStartNode(this Flow flow) =>
            flow.Nodes.FirstOrDefault(n => n.Data is StartNodeData);

        public static Node? GetNodeById(this Flow flow, Guid nodeId) =>
            flow.Nodes.FirstOrDefault(n => n.Id == nodeId);

        public static List<Edge> GetOutgoingEdges(this Flow flow, Guid nodeId) =>
            flow.Edges.Where(e => e.Source == nodeId).ToList();
    }
}