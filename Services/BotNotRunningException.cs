namespace states.Services;

// Рантайм бота в tgengine не поднят (409 + code=BOT_NOT_RUNNING). Чаще всего это гонка
// с деплоем: сервис уже принимает запросы, но боты ещё стартуют. ActionWorkerService даёт
// ровно одну повторную попытку через полминуты — если бот не поднялся и к ней, значит он
// выключен или разлогинен всерьёз, и ждать дальше бессмысленно: лид уходит на оператора.
public class BotNotRunningException : Exception
{
    public BotNotRunningException(string message) : base(message) { }
}
