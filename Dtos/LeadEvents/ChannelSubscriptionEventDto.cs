namespace states.Dtos.LeadEvents
{
    public sealed record ChannelSubscriptionEventDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        DateTime CreatedAt
    ) : LeadEventBaseDto(Id, TenantId, SpaceId, LeadId, CreatedAt);
}
