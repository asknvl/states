namespace states.Services.Events.Producer.Payloads.LeadState
{
    public record LeadDepositChangedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        decimal TotalDepositAmount,
        decimal FirstDepositAmount,
        decimal LastDepositAmount,
        int DepositCount,
        string CurrencyCode,
        long Version
    ) : LeadStateChangeEventPayloadBase(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            LeadId: LeadId,
            Version: Version);
}
