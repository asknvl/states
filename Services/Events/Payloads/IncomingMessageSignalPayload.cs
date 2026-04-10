namespace states.Services.Events.Payloads
{
    public sealed record IncomingMessageSignalPayload(
        Guid TenantId,
        Guid BotId,
        Guid ChatId,        
        Guid GlobalId
    );

}
