using states.Services.FunnelService.Application;

namespace states.Dtos.Actions
{
    public sealed record ManageTagAction(
        Guid Id,        
        TimeSpan? Delay,
        TagOperation Operation,
        Guid TagId,
        Guid? ReplacementTagId
    ) : NodeAction(
        Id: Id,        
        Delay: Delay
    );
}
