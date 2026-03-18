using states.Dtos.Funnels;

namespace states.Services.FunnelService.Application
{
    public interface IFunnelsApplicationService
    {
        Task<FunnelDto> Create(FunnelCreateDto dto, CancellationToken ct);
        Task<FunnelDto> Update(Guid funnelId, FunnelUpdateDto dto, CancellationToken ct);
        Task<Funnel> Get(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyCollection<FunnelDto>> Get(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct);
        Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct);

        Task<IReadOnlyCollection<Tag>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> AddTag(Guid funnelId, string name, CancellationToken ct);
        Task<IReadOnlyList<Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

        Task<Flow> AddFlow(Guid funnelId, Flow flow, CancellationToken ct);
        Task<Flow> UpdateFlow(Guid funnelId, Flow flow, CancellationToken ct);
        Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct);
    }
}