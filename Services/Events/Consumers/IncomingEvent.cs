namespace states.Services.Events.Consumer;

public record IncomingEvent<TPayload>(
    Guid Id,
    string Type,
    TPayload Payload,
    DateTime OccuredAt
);
