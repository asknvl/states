using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record AiReplyNodeData (
        string Label,
        LeadFunnelStatus FinishStatus,
        string? Goal,
        string? Requirements,
        string? Legend,
        string? AdditionalInfo
        ) : NodeData(
            Label: Label,
            FinishStatus: FinishStatus
        );
    
}
