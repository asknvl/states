namespace states.Services.TGEngineClient.Dtos
{
    // Тело запроса POST /actions/read в tgengine (ReadHisotryAction).
    // MaxMessageId = null — прочитать все входящие сообщения чата.
    internal record ReadHistoryDto(
       Guid TenantId,
       Guid SpaceId,
       Guid BotId,
       Guid ChatId,
       Guid? MaxMessageId);
}
