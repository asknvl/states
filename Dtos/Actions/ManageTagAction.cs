using states.Services.FunnelService.Application;

namespace states.Dtos.Actions
{
    public sealed record ManageTagAction(
        Guid Id,
        TimeSpan Delay
        ) : Action(
            Id: Id,
            Type: ActionType.ManageTag,
            Delay: Delay);
}
