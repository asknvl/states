using states.Mongo.Documents;

namespace states.Mongo.Repositories
{
    public interface IFunnelsRepository
    {
        Task Create(FunnelDocument document, CancellationToken ct);
        Task UpdateMetadata(Guid funnelId, string name, string? description, CancellationToken ct);

        Task<IReadOnlyCollection<FunnelDocument>> GetByTenant(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct);
        Task<FunnelDocument> Get(Guid funnelId);

        Task AddFlow(Guid funnelId, FlowDocument flow, CancellationToken ct);
        Task UpdateFlow(Guid funnelId, FlowDocument flow, CancellationToken ct);
        Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct);

        Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> AddTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

        Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct);

        Task<IReadOnlyCollection<FunnelDocument>> GetAllActive(CancellationToken ct);
    }
}
