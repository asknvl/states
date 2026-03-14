using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Mongo.Mappers;
using states.Mongo.Repositories;

namespace states.Services.FunnelService.Configuration
{
    public class FunnelConfigurationService : IFunnelConfigurationService
    {
        private readonly IFunnelsRepository funnelsRepository;
        private readonly ILogger logger;

        public FunnelConfigurationService(
            IFunnelsRepository funnelsRepository,
            ILogger<FunnelConfigurationService> logger)
        {
            this.funnelsRepository = funnelsRepository;
            this.logger = logger;
        }

        public async Task<FunnelDocument> Create(Funnel dto, CancellationToken ct = default)
        {
            var funnel = dto.ToDocument();
            await funnelsRepository.Create(funnel, ct);
            return funnel;
        }

        public Task<IReadOnlyCollection<FunnelDocument>> Get(Guid tenantId, Guid botId)
        {
            throw new NotImplementedException();
        }

        public Task<FunnelDocument> Get(Guid funnelId)
        {
            throw new NotImplementedException();
        }
        public Task AddTag(Guid funnelId, TagDocument tag, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<FunnelDocument> Update(Funnel dto, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
