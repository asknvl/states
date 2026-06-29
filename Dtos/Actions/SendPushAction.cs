using states.Services.FunnelService.Application;

namespace states.Dtos.Actions
{
    public sealed record SendPushAction(
        Guid Id,
        TimeSpan? Delay,
        Guid PresetId        
    ) : NodeAction(
        Id: Id,        
        Delay: Delay
    );
}
