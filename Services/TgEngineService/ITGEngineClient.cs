namespace states.Services.TgEngineService;

public interface ITGEngineClient
{
    Task SendPreset(        
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        Guid presetId,
        CancellationToken ct);
}
