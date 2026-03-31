namespace states.Dtos.Funnels;

public sealed record FunnelCreateDto(
    Guid TenantId,
    Guid SpaceId,    
    string Name,
    string? Description
);
