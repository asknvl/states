using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record AiReplyNodeData (
        string Label,
        LeadFunnelStatus FinishStatus
        ) : NodeData(
            Label: Label,
            FinishStatus: FinishStatus
        );
    
}
