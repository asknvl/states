namespace states.Services.Events.Consumers.GlobalEvents.Payloads;

public record BotSubscriptionChangedPayload(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    Guid ChatId,
    Guid GlobalId,

    // Id лида во внешнем мессенджере (для Telegram — числовой telegram id). Строка, потому что
    // формат id зависит от мессенджера. Null у событий, опубликованных до добавления поля.
    string? ExternalId,

    string? StartParameter,
    bool IsActive,
    DateTime SubscribedAt
);
