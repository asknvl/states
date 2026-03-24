using states.Dtos.Actions;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record StartNodeData(
        string Label,
        LeadFunnelStatus FinishStatus
    ) : NodeData(
        Label,
        FinishStatus
    );
}
