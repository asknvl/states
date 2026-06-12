namespace states.Services.Events.Consumer;

public enum PostbackEventType
{
    REGISTRATION,
    PURCHASE,
    REFUND,
    CHARGEBACK,
    CUSTOM,
    SALE,
    RESALE,
    COMMISSION,
    WITHDRAW_PENDING,
    WITHDRAW_FAILED,
    WITHDRAW_SUCCEEDED
}
