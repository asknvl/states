using states.Dtos.Funnels;
using states.Services.CampaignClient;
using states.Services.LeadService;

namespace states.Dtos.Leads;

/// <summary>
/// Пачка лидов, которых нужно провести через вход в воронку заранее — до их первого реального
/// сообщения (см. migrator). TenantId/SpaceId/BotId общие для всей пачки. ChatId/LeadId к этому
/// моменту уже созданы миграцией в tgengine и campaigns соответственно.
/// </summary>
public sealed record MaterializeLeadStatesRequestDto(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    IReadOnlyList<MaterializeLeadStateItemDto> Items);

/// <summary>
/// Status обязателен: EnterFunnel считает лида мигрированным именно по наличию статуса (см.
/// LeadProgressionService.EnterFunnel) — без него лид вошёл бы как органический, с задачами
/// текущей ноды, которые для уже отработавшего во внешнем сервисе лида не нужны.
/// </summary>
public sealed record MaterializeLeadStateItemDto(
    Guid ChatId,
    string? ExternalId,
    string LeadId,
    Guid? CampaignId,
    string? CampaignName,
    string? SourceId,
    string? SourceName,
    Guid? FunnelId,
    Guid? FlowId,
    Guid? NodeId,
    LeadFunnelStatus Status,
    IReadOnlyList<Tag>? Tags,

    // Custom fields лида из внешнего сервиса — станут начальными postback-параметрами
    // состояния и уедут в tgengine внутри события LeadStateCreated (см. EnterFunnelRequest).
    IReadOnlyDictionary<string, string>? CustomFields = null);

public sealed record MaterializeLeadStatesResultDto(IReadOnlyList<MaterializeLeadStateResultDto> Items);

public sealed record MaterializeLeadStateResultDto(
    Guid ChatId,
    bool Succeeded,
    string? Error);

/// <summary>
/// Полный откат проактивной миграции по боту — без частичной/выборочной логики.
/// </summary>
public sealed record DeleteMigratedLeadStatesResultDto(int DeletedCount);
