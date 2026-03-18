using states.Dtos.Funnels;

namespace states.Services.FunnelService.Application
{
    public interface IFunnelsApplicationService
    {
        Task<Funnel> Create(Funnel dto, CancellationToken ct);
        Task<Funnel> Update(Funnel dto, CancellationToken ct);
        Task<Funnel> Get(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyCollection<Funnel>> Get(Guid tenantId, Guid botId, CancellationToken ct);
        Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct);

        Task<IReadOnlyCollection<Tag>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> AddTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
        Task<IReadOnlyList<Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
    }
}