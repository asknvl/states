namespace states.Services.Events.Consumer.Payloads;

public record BotSubscriptionChangedPayload(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    Guid ChatId,
    Guid GlobalId,
    string? StartParameter,
    bool IsActive,
    DateTime SubscribedAt
);
