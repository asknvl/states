using states.Mongo.Mappers;
using states.Mongo.Repositories;
using states.Mongo.Documents;
using DTO = states.Dtos.Funnels;

namespace states.Services.FunnelService.Application
{
    public class FunnelApplicationService : IFunnelsApplicationService
    {
        private readonly IFunnelsRepository repository;

        public FunnelApplicationService(IFunnelsRepository repository)
        {
            this.repository = repository;
        }

        public async Task<DTO.Funnel> Create(DTO.Funnel dto, CancellationToken ct)
        {
            var document = dto.ToDocument();
            await repository.Create(document, ct);
            return document.ToDto();
        }

        public async Task<DTO.Funnel> Update(DTO.Funnel dto, CancellationToken ct)
        {
            var document = dto.ToDocument();
            await repository.Update(document, ct);
            return document.ToDto();
        }

        public async Task<DTO.Funnel> Get(Guid funnelId, CancellationToken ct)
        {
            var document = await repository.Get(funnelId);
            return document.ToDto();
        }

        public async Task<IReadOnlyCollection<DTO.Funnel>> Get(Guid tenantId, Guid botId, CancellationToken ct)
        {
            var documents = await repository.Get(tenantId, botId);
            return documents.Select(x => x.ToDto()).ToList();
        }

        public async Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct)
        {
            await repository.SetIsActiveState(funnelId, isActive, ct);
        }

        public async Task<IReadOnlyCollection<DTO.Tag>> GetTags(Guid funnelId, CancellationToken ct)
        {
            var tags = await repository.GetTags(funnelId, ct);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> AddTag(Guid funnelId, DTO.Tag tag, CancellationToken ct)
        {
            var document = new TagDocument { Id = tag.Id, Name = tag.Name };
            var tags = await repository.AddTag(funnelId, document, ct);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            var tags = await repository.RemoveTag(funnelId, tagId, ct);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            var tags = await repository.UpdateTag(funnelId, tagId, name, ct);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }
    }
}