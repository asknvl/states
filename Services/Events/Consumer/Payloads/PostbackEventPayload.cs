namespace states.Services.Events.Consumer.Payloads;

public sealed record PostbackEventPayload(
    string? Tid,
    PostbackEventType Type,
    string LeadId,
    decimal? Payout,
    Guid EventId,
    string? Currency,
    Guid TenantId,
    DateTime ReceivedAt,
    Dictionary<string, string>? CustomFields
);
