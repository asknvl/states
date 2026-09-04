namespace states.Services.TGEngineClient.Dtos
{
    // Тело запроса POST /actions/typing в tgengine (SendTypingAction).
    // DurationMs — сколько держать индикатор «печатает»; tgengine отвечает сразу,
    // цикл крутится у него в фоне и гаснет при любой отправке сообщения в чат.
    internal record SendTypingDto(
       Guid TenantId,
       Guid SpaceId,
       Guid BotId,
       Guid ChatId,
       int? DurationMs);
}
