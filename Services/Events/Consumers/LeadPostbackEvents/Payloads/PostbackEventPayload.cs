using states.Services.Events.Consumers.LeadPostbackEvents;

namespace states.Services.Events.Consumers.LeadPostbackEvents.Payloads;

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
