using states.Mongo.Documents.Outbox;

namespace states.Mongo.Repositories;

public interface IOutboxRepository
{
    Task<List<OutboxDocument>> ClaimBatch(int size, TimeSpan claimTimeout, CancellationToken ct);
    Task Delete(Guid id, CancellationToken ct);
}
