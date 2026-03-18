namespace states.Dtos.Funnels;

public sealed record FunnelCreateDto(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    string Name,
    string? Description
);
