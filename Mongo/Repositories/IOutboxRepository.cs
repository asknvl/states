using states.Mongo.Documents.Outbox;

namespace states.Mongo.Repositories;

public interface IOutboxRepository
{
    /// <summary>
    /// Идемпотентная постановка в очередь: документ с занятым Id (повторная обработка того же
    /// события) молча пропускается. false — уже стоит в очереди или уже доставлен.
    /// </summary>
    Task<bool> TryAdd(OutboxDocument document, CancellationToken ct);
    Task<OutboxDocument?> TakeNext(TimeSpan claimTimeout, CancellationToken ct);
    Task Delete(Guid id, CancellationToken ct);
}
