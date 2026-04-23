using states.Mongo.Documents.Outbox;

namespace states.Mongo.Repositories;

public interface IOutboxRepository
{
    Task<OutboxDocument?> TakeNext(TimeSpan claimTimeout, CancellationToken ct);
    Task Delete(Guid id, CancellationToken ct);
}
