using states.Dtos.LeadEvents;

namespace states.Services.LeadEventsService.Application
{
    public interface ILeadEventsApplicationService
    {
        Task<IReadOnlyCollection<LeadEventBaseDto>> GetLeadEvents(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes,
            CancellationToken ct = default);

        IReadOnlyCollection<LeadEventTypes> GetLeadEventTypes();

        /// <summary>
        /// Импортирует пачку исторических событий лида (см. migrator). Идемпотентно: повторный
        /// импорт того же ExternalEventId — не ошибка, просто не создаёт дубль (см. NotImported).
        /// </summary>
        Task<ImportLeadEventsResultDto> ImportEvents(ImportLeadEventsRequestDto request, CancellationToken ct = default);
    }
}
