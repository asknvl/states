namespace states.Services.Events.Payloads
{
    public record ChatDeletionPayload(
        Guid TenantId,
        Guid ChatId
    );    
}
