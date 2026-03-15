using states.Mongo.Documents;

namespace states.Mongo.Repositories
{
    public interface IFunnelsRepository
    {
        Task Create(FunnelDocument document, CancellationToken ct);
        Task Update(FunnelDocument document, CancellationToken ct);
        Task<IReadOnlyCollection<FunnelDocument>> Get(Guid tenantId, Guid botId);
        Task<FunnelDocument> Get(Guid funnelId);

        Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> AddTag(Guid funnelId, string name, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

        Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct);

        Task<IReadOnlyCollection<FunnelDocument>> GetAllActive(CancellationToken ct);
    }
}
