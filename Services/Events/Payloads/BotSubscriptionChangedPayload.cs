namespace states.Services.Events.Payloads;

public record BotSubscriptionChangedPayload(
    Guid TenantId,
    Guid BotId,
    Guid ChatId,
    Guid GlobalId,
    string? StartParameter,
    bool IsActive,
    DateTime SubscribedAt
);
