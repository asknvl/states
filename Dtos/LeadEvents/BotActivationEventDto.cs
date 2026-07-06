namespace states.Dtos.LeadEvents
{
    public sealed record BotActivationEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        DateTime CreatedAt,
        Guid BotId
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, CreatedAt);
}
