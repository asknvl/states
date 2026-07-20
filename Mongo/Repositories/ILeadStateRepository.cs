using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Repositories;

public interface ILeadStateRepository
{
    Task<FunnelLeadState> CreateLeadState(FunnelLeadState state, CancellationToken ct);
    Task<FunnelLeadState> GetLeadState(Guid leadStateId, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid chatId, CancellationToken ct);
    Task<FunnelLeadState?> ClaimWaitingLeadByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);
    Task<List<FunnelLeadState>> GetLeadStatesByLeadId(Guid tenantId, string leadId, CancellationToken ct);

    // Дополняет postbackParameters во всех FunnelLeadState с данным leadId (их может быть несколько,
    // если кампания ведёт лида через несколько ботов) значениями из очередного постбека.
    Task MergePostbackParameters(Guid tenantId, string leadId, Dictionary<string, string> parameters, CancellationToken ct);

    // Пересчитывает депозитные агрегаты (total/first/last/count/currency) из лога leadEvents
    // (SALE/RESALE) и проставляет их во все FunnelLeadState с данным leadId. Вызывается после
    // записи очередного депозитного события.
    Task RecalculateDeposits(Guid tenantId, string leadId, CancellationToken ct);

    // Уникальные (tenantId, leadId) всех лидов, у которых есть депозиты — для разового бэкфилла
    // депозитов в tgengine (переотправка событий через RecalculateDeposits).
    Task<IReadOnlyList<(Guid TenantId, string LeadId)>> GetLeadIdsWithDeposits(CancellationToken ct);
    //Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct);

    Task SetLeadFunnelPosition(
        Guid leadStateId,
        Guid funnelId,
        string funnelName,
        Guid flowId,
        string flowName,
        Guid nodeId,
        string nodeLabel,
        LeadFunnelStatus status,
        List<ActionStatusEntry> actions,
        Guid? exitEdgeId,
        CancellationToken ct);

    Task UpdateActionStatus(Guid leadStateId, Guid nodeId, Guid actionId, ActionStatus status, CancellationToken ct, string? errorMessage = null);
    Task MarkPushCompleted(Guid leadStateId, Guid pushId, CancellationToken ct);
    Task UpdateTag(Guid leadStateId, TagOperation operation, Tag tagId, Tag replacementTag, CancellationToken ct);    
    Task UpdateLeadStateStatus(Guid leadStateId, LeadFunnelStatus status, CancellationToken ct);

    // Возвращает лида в Waiting, только если он всё ещё стоит на ноде nodeId со статусом Nothing.
    // Если лид уже переведён на другую ноду (или статус сменили на Manual/Blocked) — ничего не пишет.
    Task TrySetWaitingIfStillOnNode(Guid leadStateId, Guid nodeId, CancellationToken ct);
    Task SetIsTranslatorOn(Guid leadStateId, bool? isInputTranslatorOn, bool? isOutputTranslatorOn, CancellationToken ct);

    Task SaveTags(Guid leadStateId, List<Tag> tags, CancellationToken ct);
    Task UpdateLeadStateStatusByChatId(Guid chatId, LeadFunnelStatus status, CancellationToken ct);

    // Атомарно помечает первый контакт лида (FirstContactAt == null → now). Возвращает обновлённый
    // документ только при первом вызове; если контакт уже был или лид не найден — null.
    Task<FunnelLeadState?> TryMarkFirstContact(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);

    Task<FunnelLeadState?> MarkBlockedByChatId(Guid chatId, CancellationToken ct);
    Task<FunnelLeadState?> UnblockByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);
    Task ResetCurrentNodeActions(Guid leadStateId, List<ActionStatusEntry> actions, CancellationToken ct);

    Task<bool> AreAllActionsCompleted(Guid leadStateId, Guid nodeId, CancellationToken ct);    
    Task Delete(Guid leadStateId, CancellationToken ct);
}
