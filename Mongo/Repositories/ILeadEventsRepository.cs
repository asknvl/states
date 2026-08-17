using states.Mongo.Documents.LeadEvents;
using states.Services.LeadEventsService.Application;

namespace states.Mongo.Repositories
{
    public interface ILeadEventsRepository
    {
        Task<IReadOnlyCollection<LeadEventBaseDocument>> GetByLead(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes,
            CancellationToken ct = default);

        Task Create(LeadEventBaseDocument document, CancellationToken ct = default);

        /// <summary>
        /// Батч-вставка (unordered) для импорта исторических событий. Документы, отклонённые
        /// уникальным индексом (tenantId, eventId) — повтор/резюме прогона миграции — тихо
        /// пропускаются, остальные вставляются. Возвращает реально вставленные документы.
        /// </summary>
        Task<IReadOnlyList<LeadEventBaseDocument>> CreateMany(
            IReadOnlyList<LeadEventBaseDocument> documents,
            CancellationToken ct = default);
    }
}
