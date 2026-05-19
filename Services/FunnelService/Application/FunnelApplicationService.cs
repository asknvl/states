using states.Dtos.Funnels;
using states.Dtos.Nodes;
using states.Mongo.Mappers;
using states.Mongo.Repositories;

namespace states.Services.FunnelService.Application
{
    public class FunnelApplicationService : IFunnelsApplicationService
    {
        private readonly IFunnelsRepository funnelsRepository;
        private readonly IFunnelRuntimeSupervisor runtimeSupervisor;
        private readonly IFoldersRepository foldersRepository;
        private readonly ITenantTagsRepository tenantTagsRepository;

        public FunnelApplicationService(
            IFunnelsRepository repository,
            IFunnelRuntimeSupervisor runtimeSupervisor,
            IFoldersRepository foldersRepository,
            ITenantTagsRepository tenantTagsRepository)
        {
            this.funnelsRepository = repository;
            this.runtimeSupervisor = runtimeSupervisor;
            this.foldersRepository = foldersRepository;
            this.tenantTagsRepository = tenantTagsRepository;
        }

        #region funnels
        public async Task<FunnelDto> Create(FunnelCreateDto dto, CancellationToken ct)
        {
            var funnel = dto.ToDocument();

            var folder = await foldersRepository.Create(
                tenantId: dto.TenantId,
                spaceId: dto.SpaceId,
                funnelId: funnel.Id,
                name: funnel.Name,
                order: 0,
                isHidden: true,
                ct: ct);

            funnel.PresetsFolderId = folder.Id;            

            await funnelsRepository.Create(funnel, ct);
            runtimeSupervisor.NotifyCreated(funnel.ToDto());
            return funnel.ToFunnelDto();
        }

        public async Task<FunnelDto> Update(Guid funnelId, FunnelUpdateDto dto, CancellationToken ct)
        {
            await funnelsRepository.UpdateMetadata(funnelId, dto.Name, dto.Description, ct);
            await RefreshCache(funnelId);
            var document = await funnelsRepository.Get(funnelId);
            return document.ToFunnelDto();
        }

        public async Task<Funnel> Get(Guid funnelId, CancellationToken ct)
        {
            var document = await funnelsRepository.Get(funnelId);
            return document.ToDto();
        }

        public async Task<IReadOnlyCollection<FunnelDto>> Get(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct)
        {
            var documents = await funnelsRepository.GetFunnels(tenantId, spaceId, botId, ct);
            return documents.Select(x => x.ToFunnelDto()).ToList();
        }

        public async Task<IReadOnlyCollection<FunnelShortDto>> GetShort(Guid tenantId, Guid? spaceId, CancellationToken ct)
        {
            var documents = await funnelsRepository.GetFunnels(tenantId, spaceId, null, ct);
            return documents.Select(x => new FunnelShortDto(x.Id, x.Name)).ToList();
        }

        public async Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct)
        {
            await funnelsRepository.SetIsActiveState(funnelId, isActive, ct);

            if (isActive)
                runtimeSupervisor.NotifyActivated(funnelId);
            else
                runtimeSupervisor.NotifyDeactivated(funnelId);
        }

        public async Task SetIsTranslatorOn(Guid funnelId, bool? isInputTranslatorOn, bool? isOutputTranslatorOn, CancellationToken ct)
        {
            await funnelsRepository.SetIsTranslatorOn(funnelId, isInputTranslatorOn, isOutputTranslatorOn, ct);
            await RefreshCache(funnelId);
        }

        public async Task SetRecognition(Guid funnelId, RecognitionType? photoRecognition, RecognitionType? videoRecognition, RecognitionType? voiceRecognition, CancellationToken ct)
        {
            await funnelsRepository.SetRecognition(funnelId, photoRecognition, videoRecognition, voiceRecognition, ct);
            await RefreshCache(funnelId);
        }

        public async Task SetDelays(Guid funnelId, int? readDelay, int? replyDelay, CancellationToken ct)
        {
            await funnelsRepository.SetDelays(funnelId, readDelay, replyDelay, ct);
            await RefreshCache(funnelId);
        }
        #endregion

        #region tags
        public async Task<IReadOnlyCollection<Tag>> GetTagsByFunnel(Guid funnelId, CancellationToken ct)
        {
            var tags = await funnelsRepository.GetTags(funnelId, ct);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyCollection<Tag>> GetTagsByTenant(Guid tenantId, Guid spaceId, CancellationToken ct)
        {
            var tenantTags = await tenantTagsRepository.GetTenantTags(tenantId, spaceId);
            return tenantTags
                .Select(t => new Tag(t.TagId, t.TagName))
                .ToList();
        }

        public async Task<IReadOnlyList<Tag>> AddTag(Guid funnelId, string name, CancellationToken ct)
        {
            var funnel = await funnelsRepository.Get(funnelId);

            var (tagId, tagName) = await tenantTagsRepository.CreateIfNeed(
                funnel.TenantId,
                funnel.SpaceId,
                funnelId,
                name);


            var tags = await funnelsRepository.AddTag(funnelId, tagId, tagName, ct);
            await RefreshCache(funnelId);

            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct)
        {
            var tags = await funnelsRepository.RemoveTag(funnelId, tagId, ct);
            await tenantTagsRepository.DecreaseTenantTagUsage(tagId, funnelId);
            await RefreshCache(funnelId);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }

        public async Task<IReadOnlyList<Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct)
        {
            var tags = await funnelsRepository.UpdateTag(funnelId, tagId, name, ct);
            await tenantTagsRepository.UpdateTenantTagName(tagId, name);
            await RefreshCache(funnelId);
            return tags.Select(t => new Tag(t.Id, t.Name)).ToList();
        }
        #endregion

        #region flows
        public async Task<Flow> GetFlow(Guid funnelId, Guid flowId, CancellationToken ct)
        {
            var document = await funnelsRepository.Get(funnelId);
            var flow = document.Flows.FirstOrDefault(f => f.Id == flowId)
                ?? throw new KeyNotFoundException($"Flow '{flowId}' not found in funnel '{funnelId}'.");
            return flow.ToDto();
        }

        public async Task<IReadOnlyCollection<FlowShortDto>> GetFlowsShort(Guid funnelId, CancellationToken ct)
        {
            var document = await funnelsRepository.Get(funnelId);
            return document.Flows.Select(f => new FlowShortDto(f.Id, f.Name)).ToList();
        }
        public async Task<Flow> AddFlow(Guid funnelId, Flow flow, CancellationToken ct)
        {
            var document = flow.ToDocument();
            await funnelsRepository.AddFlow(funnelId, document, ct);
            await RefreshCache(funnelId);
            return document.ToDto();
        }

        public async Task<Flow> UpdateFlow(Guid funnelId, Flow flow, CancellationToken ct)
        {
            var document = flow.ToDocument();
            await funnelsRepository.UpdateFlow(funnelId, document, ct);
            await RefreshCache(funnelId);
            return document.ToDto();
        }

        public async Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct)
        {
            await funnelsRepository.RemoveFlow(funnelId, flowId, ct);
            await RefreshCache(funnelId);
        }
        public async Task<IReadOnlyCollection<NodeShortDto>> GetNodesShort(Guid funnelId, Guid flowId, CancellationToken ct)
        {
            var document = await funnelsRepository.Get(funnelId);
            var flow = document.Flows.FirstOrDefault(f => f.Id == flowId)
                ?? throw new KeyNotFoundException($"Flow '{flowId}' not found in funnel '{funnelId}'.");
            return flow.Nodes.Select(n => new NodeShortDto(n.Id, n.Data.Label)).ToList();
        }
        #endregion

        private async Task RefreshCache(Guid funnelId)
        {
            var document = await funnelsRepository.Get(funnelId);
            runtimeSupervisor.NotifyUpdated(document.ToDto());
        }
    }
}