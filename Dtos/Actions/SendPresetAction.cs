using states.Services.FunnelService.Application;

namespace states.Dtos.Actions
{
    public sealed record SendPresetAction(
        Guid Id,
        TimeSpan? Delay,
        Guid PresetId,
        bool NeedPin
    ) : NodeAction(
        Id: Id,        
        Delay: Delay
    );
}
