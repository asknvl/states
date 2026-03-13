using states.Dtos.Actions;
using states.Services.FunnelService.Application;

namespace states.Dtos.Nodes
{   
    public sealed record ManageTagNodeData(
        string Label,
        TagOperation Operation,
        Guid TagId,
        Guid? ReplacementTagId
    ) : NodeData(
        Label               
    );    
}
