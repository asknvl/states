using states.Services.TGEngineClient.Dtos;

namespace states.Services.TgEngineService;

public interface ITGEngineClient
{
    Task SendPreset(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        Guid funnelId,
        Guid presetId,
        CancellationToken ct);

    Task<List<ChatContextMessageDto>> GetContextMessages(
        Guid tenantId,
        Guid botId,
        Guid chatId,
        int lastMessagesNumber,
        bool isImageDetailed,
        CancellationToken ct);
}
