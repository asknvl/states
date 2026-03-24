using states.Dtos.Actions;
using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record SendPresetNodeData(
        string Label,
        LeadFunnelStatus FinishStatus,
        List<SendPresetAction> Actions
    ) : NodeData(
        Label,
        FinishStatus
    );

}
