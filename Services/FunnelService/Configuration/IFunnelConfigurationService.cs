using states.Dtos.Funnels;
using states.Mongo.Documents;

namespace states.Services.FunnelService.Configuration
{
    public interface IFunnelConfigurationService
    {
        Task<FunnelDocument> Create(Funnel dto, CancellationToken ct = default);
        Task<FunnelDocument> Update(Funnel dto, CancellationToken ct = default);
        Task<IReadOnlyCollection<FunnelDocument>> Get(Guid tenantId, Guid botId);
        Task<FunnelDocument> Get(Guid funnelId);
        Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct);
        Task AddTag(Guid funnelId, TagDocument tag, CancellationToken ct);
        Task RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
        Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct);

    }
}
