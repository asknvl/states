using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public interface IActionTaskRepository
{
    Task CreateMany(IEnumerable<ActionTaskDocument> tasks, CancellationToken ct);
    Task<ActionTaskDocument?> ClaimNext(CancellationToken ct);
    Task Complete(Guid taskId, CancellationToken ct);
    Task Fail(Guid taskId, CancellationToken ct);
    Task<List<ActionTaskDocument>> GetByLeadAndNode(Guid leadStateId, Guid nodeId, CancellationToken ct);
}
