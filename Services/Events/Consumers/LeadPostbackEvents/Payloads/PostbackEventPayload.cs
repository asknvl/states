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
    Dictionary<string, string>? CustomFields,
    // Статусы от трекера: DUPLICATE — повторная доставка уже принятого события (тот же eventId),
    // REPEAT — повторное событие лида (например, вторая регистрация) со своим eventId.
    // Оба пишутся в lead_events, но воронку не двигают и конверсий в ФБ не рождают.
    // Если поле в сообщении отсутствует, считаем событие принятым.
    PostbackEventStatus Status = PostbackEventStatus.ACCEPTED
);
