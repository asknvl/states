using states.Mongo.Mappers;
using states.Mongo.Repositories;
using DTO = states.Dtos.Funnels;

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

        public async Task<DTO.Funnel> Create(DTO.Funnel dto, CancellationToken ct)
        {
            var document = dto.ToDocument();
            await repository.Create(document, ct);
            var result = document.ToDto();
            runtimeSupervisor.NotifyCreated(result);
            return result;
        }

        public async Task<DTO.Funnel> Update(DTO.Funnel dto, CancellationToken ct)
        {
            var document = dto.ToDocument();
            await repository.Update(document, ct);
            var result = document.ToDto();
            runtimeSupervisor.NotifyUpdated(result);
            return result;
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

            if (isActive)
                runtimeSupervisor.NotifyActivated(funnelId);
            else
                runtimeSupervisor.NotifyDeactivated(funnelId);
        }

        public async Task<IReadOnlyCollection<DTO.Tag>> GetTags(Guid funnelId, CancellationToken ct)
        {
            var tags = await repository.GetTags(funnelId, ct);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> AddTag(Guid funnelId, string name, CancellationToken ct)
        {
            var tags = await repository.AddTag(funnelId, name, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            var tags = await repository.RemoveTag(funnelId, tagId, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<DTO.Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            var tags = await repository.UpdateTag(funnelId, tagId, name, ct);
            await RefreshCache(funnelId);
            return tags.Select(t => new DTO.Tag(t.Id, t.Name)).ToList();
        }

        private async Task RefreshCache(Guid funnelId)
        {
            var document = await repository.Get(funnelId);
            runtimeSupervisor.NotifyUpdated(document.ToDto());
        }
    }
}