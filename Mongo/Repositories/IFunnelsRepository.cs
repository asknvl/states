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
        Task AddTag(Guid funnelId, TagDocument tag, CancellationToken ct);
        Task RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

    }
}
