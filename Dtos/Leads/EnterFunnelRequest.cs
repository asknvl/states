using states.Dtos.Funnels;
using states.Services.CampaignClient;

namespace states.Dtos.Leads;

public sealed record EnterFunnelRequest(
    Guid TenantId,
    Guid SpaceId,
    Guid BotId,
    Guid ChatId,
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
    List<Tag>? Tags = null
);
