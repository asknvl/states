using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    /// <summary>
    /// Пачка исторических событий лида, перенесённых из внешнего сервиса (см. migrator).
    /// TenantId/SpaceId общие для всей пачки.
    /// </summary>
    public sealed record ImportLeadEventsRequestDto(
        Guid TenantId,
        Guid SpaceId,
        IReadOnlyList<ImportLeadEventItemDto> Items);

    /// <summary>
    /// Type ограничен тем, что реально можно записать (см. LeadEventsApplicationService.BuildDocument):
    /// Registration/Sale/Resale. Остальные значения LeadEventTypes для импорта не поддержаны — либо
    /// не несут информации трекера, либо у них ещё нет документного типа в states — и пропускаются.
    /// Amount/CurrencyCode используются только для Sale/Resale (сумма депозита) и CurrencyCode —
    /// также для Registration; для прочих типов игнорируются.
    /// </summary>
    public sealed record ImportLeadEventItemDto(
        string LeadId,
        Guid ExternalEventId,
        LeadEventTypes Type,
        DateTime OccurredAt,
        decimal Amount,
        string? CurrencyCode);

    public sealed record ImportLeadEventsResultDto(
        int Imported,

        // Дубли (повторный/резюмированный прогон миграции) — см. уникальный индекс (tenantId, eventId).
        int NotImported,

        // Type не входит в поддерживаемый для импорта набор.
        int Skipped);
}
