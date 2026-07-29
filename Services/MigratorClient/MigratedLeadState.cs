using states.Services.LeadService;

namespace states.Services.MigratorService;

/// <summary>
/// Состояние лида во внешнем сервисе на момент выгрузки. Статус migrator приводит
/// к нашему LeadFunnelStatus сам, теги отдаёт уже переведёнными в наши идентификаторы.
/// </summary>
public record MigratedLeadState(
    LeadFunnelStatus Status,
    List<MigratedLeadTag> Tags);
