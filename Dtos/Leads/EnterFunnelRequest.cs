using states.Dtos.Funnels;
using states.Services.CampaignClient;
using states.Services.LeadService;

namespace states.Dtos.Leads;

public sealed record EnterFunnelRequest(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    Guid ChatId,

    // Id лида во внешнем мессенджере (для Telegram — числовой telegram id), формат зависит
    // от мессенджера.
    string? ExternalId,

    string LeadId,
    Guid? CampaignId,
    string? CampaignName,
    string? SourceId,
    string? SourceName,
    Guid? FunnelId,
    Guid? FlowId,
    Guid? NodeId,

    string? StartParameter,

    MigrationFrom? MigrationFrom,

    // Теги мигрированного лида, перенесённые из внешнего сервиса, — проставляются
    // лиду сразу при входе в воронку.
    List<Tag>? Tags = null,

    // Статус, с которым мигрированный лид стоял во внешнем сервисе. Задан — перекрывает
    // статус, который дала бы нода входа: лид продолжает с того же состояния, что и там.
    LeadFunnelStatus? Status = null,

    // Начальные postback-параметры лида (перенесены миграцией: custom fields внешнего
    // сервиса). Кладутся прямо в создаваемый FunnelLeadState и уезжают в tgengine внутри
    // события LeadStateCreated — БЕЗ отдельного события на каждого лида: при массовой
    // материализации это удержало бы волну в outbox/Kafka от роста в полтора раза.
    IReadOnlyDictionary<string, string>? PostbackParameters = null
);
