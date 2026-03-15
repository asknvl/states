using states.Dtos.Funnels;

namespace states.Services.FunnelService
{
    public interface IFunnelRuntimeSupervisor
    {
        void NotifyCreated(Funnel funnel);
        void NotifyUpdated(Funnel funnel);
        void NotifyDeleted(Guid funnelId);
        void NotifyActivated(Guid funnelId);
        void NotifyDeactivated(Guid funnelId);
    }
}
