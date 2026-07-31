namespace states.Services;

// Telegram отказался выполнить операцию (FLOOD_WAIT, USER_IS_BLOCKED, PEER_ID_INVALID и т.п.).
// tgengine отдаёт такие отказы как 422 с кодом в теле. В отличие от TransientActionException
// ретраить бессмысленно — повтор упрётся в тот же лимит или ту же блокировку, поэтому
// ActionWorkerService валит таску сразу и уводит лида на оператора (Manual).
public class TelegramActionException : Exception
{
    // Лид заблокировал бота. Особый случай: оператору такой лид бесполезен — писать некуда,
    // а в Blocked его переведёт событие деактивации бота из tgengine (см. GlobalEventProcessor).
    public const string UserIsBlockedCode = "USER_IS_BLOCKED";

    public TelegramActionException(string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }

    // Нормализованный код Telegram из tgengine: FLOOD_WAIT, USER_IS_BLOCKED, INTERNAL_ERROR...
    public string Code { get; }
}
