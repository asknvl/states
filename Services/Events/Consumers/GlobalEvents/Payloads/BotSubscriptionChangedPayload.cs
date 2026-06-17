namespace states.Services.Events.Consumers.GlobalEvents.Payloads;

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
