using states.Dtos.Actions;
using states.Services.FunnelService.Application;

namespace states.Dtos.Nodes
{
    public sealed record StartNodeData(
        string Label        
    ) : NodeData(
        Label
    );
}
