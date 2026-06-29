using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public interface IPushTaskRepository
{
    Task CreateMany(IEnumerable<PushTaskDocument> tasks, CancellationToken ct);
    Task<PushTaskDocument?> ClaimNext(CancellationToken ct);
    Task Complete(Guid taskId, CancellationToken ct);
    Task Fail(Guid taskId, CancellationToken ct);
    Task CancelPendingByLead(Guid leadStateId, CancellationToken ct);
    Task UnlockNext(Guid leadStateId, Guid nodeId, int completedOrder, CancellationToken ct);
}
