using states.Dtos.Leads;

namespace states.Services.LeadMigration;

public interface ILeadMigrationService
{
    /// <summary>
    /// Проводит пачку лидов через вход в воронку заранее, до их первого реального сообщения.
    /// Идемпотентно по уникальному индексу (tenantId, funnelId, leadId) на lead_states — см.
    /// LeadProgressionService.EnterFunnel.
    /// </summary>
    Task<MaterializeLeadStatesResultDto> MaterializeLeadStates(
        MaterializeLeadStatesRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Полный откат: удаляет все lead-states (+ их action/push-таски и lead-события), заведённые
    /// MaterializeLeadStates, по (tenantId, botId). Без частичного/выборочного отката.
    /// </summary>
    Task<DeleteMigratedLeadStatesResultDto> DeleteMigrated(Guid tenantId, Guid botId, CancellationToken ct = default);
}
