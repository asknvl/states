using DTO = states.Dtos.Funnels;

namespace states.Services.FunnelService.Application
{
    public interface IFunnelsApplicationService
    {
        Task<DTO.Funnel> Create(DTO.Funnel dto, CancellationToken ct);
        Task<DTO.Funnel> Update(DTO.Funnel dto, CancellationToken ct);
        Task<DTO.Funnel> Get(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyCollection<DTO.Funnel>> Get(Guid tenantId, Guid botId, CancellationToken ct);
        Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct);

        Task<IReadOnlyCollection<DTO.Tag>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<DTO.Tag>> AddTag(Guid funnelId, DTO.Tag tag, CancellationToken ct);
        Task<IReadOnlyList<DTO.Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<DTO.Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
    }
}