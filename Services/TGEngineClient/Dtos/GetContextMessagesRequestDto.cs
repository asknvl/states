namespace states.Services.TGEngineClient.Dtos
{
    public sealed record GetContextMessagesRequestDto(
        Guid TenantId,
        Guid BotId,
        Guid ChatId,
        int LastMessagesNumber,
        bool returnFromLastOutcoming,
        bool IsImageDetailed);
}
