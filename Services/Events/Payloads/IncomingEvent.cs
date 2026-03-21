namespace states.Services.Events.Payloads;

public record IncomingEvent<TPayload>(
    Guid Id,
    string Type,
    TPayload Payload,
    DateTime OccuredAt
);
