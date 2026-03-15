using states.Dtos.Funnels;
using states.Mongo.Mappers;
using states.Mongo.Repositories;
using states.Services.FunnelService;

namespace states.Services.FunnelService.Runtime
{
    public sealed class FunnelRuntimeService : IFunnelRuntimeSupervisor, IHostedService
    {
        private readonly FunnelCache cache;
        private readonly IFunnelsRepository repository;
        private readonly ILogger<FunnelRuntimeService> logger;

        public FunnelRuntimeService(
            FunnelCache cache,
            IFunnelsRepository repository,
            ILogger<FunnelRuntimeService> logger)
        {
            this.cache = cache;
            this.repository = repository;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var documents = await repository.GetAllActive(ct);

            foreach (var document in documents)
            {
                cache.Set(document.ToDto());
            }

            logger.LogInformation("FunnelRuntimeService started, loaded {Count} active funnels", documents.Count);
        }

        public Task StopAsync(CancellationToken ct)
        {
            logger.LogInformation("FunnelRuntimeService stopped");
            return Task.CompletedTask;
        }

        public void NotifyCreated(Funnel funnel)
        {
            if (funnel.IsActive != false)
                cache.Set(funnel);
        }

        public void NotifyUpdated(Funnel funnel)
        {
            if (funnel.IsActive != false)
                cache.Set(funnel);
            else
                cache.Remove(funnel.Id);
        }

        public void NotifyDeleted(Guid funnelId)
        {
            cache.Remove(funnelId);
        }

        public async void NotifyActivated(Guid funnelId)
        {
            var document = await repository.Get(funnelId);
            cache.Set(document.ToDto());
        }

        public void NotifyDeactivated(Guid funnelId)
        {
            cache.Remove(funnelId);
        }
    }
}
