using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Mongo.Repositories;

namespace states.Services.FunnelService
{
    // Истина по именам тегов — tenant_tags; имена, оставшиеся в документах воронок, — рудимент
    // старых записей, они не читаются. Тег без документа в tenant_tags не отображаем:
    // имени для него не существует.
    public class FunnelTagResolver(ITenantTagsRepository tenantTagsRepository)
    {
        public async Task<List<Tag>> Resolve(Guid tenantId, IReadOnlyCollection<TagDocument> tags)
        {
            if (tags.Count == 0)
                return [];

            var tenantTags = await tenantTagsRepository.GetByIds(tenantId, tags.Select(t => t.Id).ToList());
            var names = tenantTags.ToDictionary(t => t.TagId, t => t.TagName);

            return tags
                .Where(t => names.ContainsKey(t.Id))
                .Select(t => new Tag(t.Id, names[t.Id]))
                .ToList();
        }

        public Task<List<Tag>> Resolve(FunnelDocument document)
            => Resolve(document.TenantId, document.Tags);
    }
}
