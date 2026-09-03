using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public interface IActionTaskRepository
{
    Task CreateMany(IEnumerable<ActionTaskDocument> tasks, CancellationToken ct);
    Task<ActionTaskDocument?> ClaimNext(CancellationToken ct);
    Task<ActionTaskDocument?> ClaimAbandoned(CancellationToken ct);
    Task Complete(Guid taskId, CancellationToken ct);
    Task Fail(Guid taskId, CancellationToken ct);

    // Отмена конкретной таски, уже забранной в работу: гвард воркера по статусу лида
    // (Manual/Blocked/удалён) закрывает её как Cancelled вместо выполнения.
    Task Cancel(Guid taskId, CancellationToken ct);
    Task Reschedule(Guid taskId, DateTime nextAttemptAt, CancellationToken ct);
    Task<List<ActionTaskDocument>> GetByLeadAndNode(Guid leadStateId, Guid nodeId, CancellationToken ct);
    Task CancelPendingByLeadAndNode(Guid leadStateId, Guid nodeId, CancellationToken ct);

    // includeMarkRead: обычные переходы отложенную прочитку не отменяют (см. комментарий
    // в реализации), ручной перевод в Manual — глушит и её.
    Task CancelPendingByLead(Guid leadStateId, CancellationToken ct, bool includeMarkRead = false);
    Task UnlockNext(Guid leadStateId, Guid nodeId, int completedOrder, CancellationToken ct);
    Task TryInsertAiReplyTask(AiReplyActionTaskDocument task, CancellationToken ct);
    Task UpsertPendingAiRouterTask(AiRouterActionTaskDocument task, CancellationToken ct);
    Task UpsertPendingMarkReadTask(MarkReadActionTaskDocument task, CancellationToken ct);
    Task TryInsertSendTypingTask(SendTypingActionTaskDocument task, CancellationToken ct);
}
