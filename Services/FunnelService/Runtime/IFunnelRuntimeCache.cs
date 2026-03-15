using states.Dtos.Funnels;

namespace states.Services.FunnelService.Runtime
{
    public interface IFunnelRuntimeCache
    {
        Funnel? GetFunnel(Guid funnelId);        
    }
}
