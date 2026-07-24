namespace states.Services;

// Временная ошибка внешнего сервиса (сеть, таймаут, 408/429/5xx, оборванный ответ):
// действие имеет смысл повторить. ActionWorkerService по этому типу перепланирует таску
// с экспоненциальной задержкой вместо немедленного перевода лида в Manual.
public class TransientActionException : Exception
{
    public TransientActionException(string message, Exception? inner = null)
        : base(message, inner) { }
}
