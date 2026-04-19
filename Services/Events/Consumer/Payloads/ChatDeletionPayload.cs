namespace states.Services.Events.Consumer.Payloads
{
    public record ChatDeletionPayload(
        Guid TenantId,
        Guid ChatId
    );    
}
