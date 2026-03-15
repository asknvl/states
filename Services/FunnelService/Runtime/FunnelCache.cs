using Microsoft.Extensions.Caching.Memory;
using states.Dtos.Funnels;

namespace states.Services.FunnelService.Runtime
{
    public sealed class FunnelCache : IFunnelRuntimeCache
    {
        private readonly IMemoryCache cache;

        private static string CacheKey(Guid funnelId) => $"funnel:{funnelId}";

        public FunnelCache(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public Funnel? GetFunnel(Guid funnelId)
        {
            cache.TryGetValue(CacheKey(funnelId), out Funnel? funnel);
            return funnel;
        }

        public void Set(Funnel funnel)
        {
            cache.Set(CacheKey(funnel.Id), funnel);
        }

        public void Remove(Guid funnelId)
        {
            cache.Remove(CacheKey(funnelId));
        }
    }
}
