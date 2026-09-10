using MongoDB.Driver;
using states.Mongo.Documents.TenantTags;

namespace states.Mongo.Repositories
{
    public interface ITenantTagsRepository
    {
        Task<(Guid tagId, string name)> CreateIfNeed(Guid tenantId, Guid spaceId, Guid funnelId, string tagName);
        Task<IReadOnlyCollection<TenantTag>> GetByIds(Guid tenantId, IReadOnlyCollection<Guid> tagIds);
        Task UpdateTenantTagName(Guid tenantId, Guid tagId, string name);
        Task DecreaseTenantTagUsage(Guid tagId, Guid funnelId);
        
    }
}
