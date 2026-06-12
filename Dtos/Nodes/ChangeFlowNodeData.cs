using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record ChangeFlowNodeData(
        string Label,
        LeadFunnelStatus FinishStatus,
        Guid FlowId,
        Guid NodeId
        ) : NodeData(
            Label: Label,
            FinishStatus: FinishStatus);
    
}
