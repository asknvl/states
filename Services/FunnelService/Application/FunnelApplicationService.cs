using states.Dtos.Funnels;
using states.Mongo.Mappers;
using states.Mongo.Repositories;

namespace states.Services.FunnelService.Application
{
    public class FunnelApplicationService : IFunnelsApplicationService
    {
        private readonly IFunnelsRepository repository;
        private readonly IFunnelRuntimeSupervisor runtimeSupervisor;

        public FunnelApplicationService(
            IFunnelsRepository repository,
            IFunnelRuntimeSupervisor runtimeSupervisor)
        {
            this.repository = repository;
            this.runtimeSupervisor = runtimeSupervisor;
        }

        public async Task<FunnelDto> Create(FunnelCreateDto dto, CancellationToken ct)
        {
            var document = dto.ToDocument();
            await repository.Create(document, ct);
            runtimeSupervisor.NotifyCreated(document.ToDto());
            return document.ToFunnelDto();
        }

        public async Task<FunnelDto> Update(Guid funnelId, FunnelUpdateDto dto, CancellationToken ct)
        {
            await repository.UpdateMetadata(funnelId, dto.Name, dto.Description, ct);
            await RefreshCache(funnelId);
            var document = await repository.Get(funnelId);
            return document.ToFunnelDto();
        }

        public async Task<Funnel> Get(Guid funnelId, CancellationToken ct)
        {
            var document = await repository.Get(funnelId);
            return document.ToDto();
        }

        public async Task<IReadOnlyCollection<FunnelDto>> Get(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct)
        {
            var documents = await repository.GetByTenant(tenantId, spaceId, botId, ct);
            return documents.Select(x => x.ToFunnelDto()).ToList();
        }

        public async Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct)
        {
            await repository.SetIsActiveState(funnelId, isActive, ct);

            if (isActive)
                runtimeSupervisor.NotifyActivated(funnelId);
            else
                runtimeSupervisor.NotifyDeactivated(funnelId);
        }

        public async Task<IReadOnlyCollection<Tag>> GetTags(Guid funnelId, CancellationToken ct)
        {
            var tags = await repository.GetTags(funnelId, ct);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<Tag>> AddTag(Guid funnelId, string name, CancellationToken ct)
        {
            var tagId = Guid.CreateVersion7();
            var tags = await repository.AddTag(funnelId, tagId, name, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            var tags = await repository.RemoveTag(funnelId, tagId, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            var tags = await repository.UpdateTag(funnelId, tagId, name, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<Flow> AddFlow(Guid funnelId, Flow flow, CancellationToken ct)
        {
            var document = flow.ToDocument();
            await repository.AddFlow(funnelId, document, ct);
            await RefreshCache(funnelId);
            return document.ToDto();
        }

        public async Task<Flow> UpdateFlow(Guid funnelId, Flow flow, CancellationToken ct)
        {
            var document = flow.ToDocument();
            await repository.UpdateFlow(funnelId, document, ct);
            await RefreshCache(funnelId);
            return document.ToDto();
        }

        public async Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct)
        {
            await repository.RemoveFlow(funnelId, flowId, ct);
            await RefreshCache(funnelId);
        }

        private async Task RefreshCache(Guid funnelId)
        {
            var document = await repository.Get(funnelId);
            runtimeSupervisor.NotifyUpdated(document.ToDto());
        }
    }
}