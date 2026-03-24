using states.Dtos.Actions;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Dtos.Nodes
{   
    public sealed record ManageTagNodeData(
        string Label,
        LeadFunnelStatus FinishStatus,
        List<ManageTagAction> Actions        
    ) : NodeData(
        Label,
        FinishStatus
    );    
}
