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

    Task<ChatContextResponse> GetChatContext(
        Guid tenantId,
        Guid botId,
        Guid chatId,
        int limit,
        CancellationToken ct);
}
