namespace states.Services.Events.Producer.Payloads.Conversions
{
    /// <summary>
    /// Конверсия лида для ФБ-пайплайна: states публикует в lead-conversion-events,
    /// campaigns обогащает данными клика/кампании/источника и отправляет в fb-events.
    /// ConversionEventId — детерминированный id конверсии (для постбэков — EventId трекера,
    /// для контакта — Id лид-стейта): по нему дедуплицируют campaigns и Meta CAPI.
    /// Id конверта для этого не годится — он генерируется заново при каждом Publish.
    /// </summary>
    public record LeadConversionPayload(
        Guid ConversionEventId,
        LeadConversionType Type,
        Guid TenantId,
        Guid SpaceId,
        Guid CampaignId,
        string LeadId,
        DateTime OccurredAt,
        decimal? Amount,
        string? Currency);
}
