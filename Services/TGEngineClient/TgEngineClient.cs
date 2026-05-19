using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace states.Services.TgEngineService;

public class TGEngineClient : ITGEngineClient
{
    private readonly HttpClient http;
    private readonly ILogger logger;

    public TGEngineClient(HttpClient http, ILogger<TGEngineClient> logger)
    {
        this.http = http;
        this.logger = logger;
    }

    public async Task SendPreset(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        Guid funnelId,
        Guid presetId,        
        CancellationToken ct)
    {
        var body = new SendPresetRequest(
            tenantId,
            spaceId,
            botId,
            chatId,
            funnelId,
            presetId);

        HttpResponseMessage response;

        try
        {
            response = await http.PostAsJsonAsync("/botpresets/send", body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TgEngineClient SendPreset failed: presetId={PresetId}", presetId);
            throw;
        }

        response.EnsureSuccessStatusCode();
    }

    // TODO: уточнить реальный endpoint у TgEngine
    public async Task<ChatContextResponse> GetChatContext(
        Guid tenantId,
        Guid botId,
        Guid chatId,
        int limit,
        CancellationToken ct)
    {
        var url = $"/chats/context?tenantId={tenantId}&botId={botId}&chatId={chatId}&limit={limit}";

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TgEngineClient GetChatContext failed: chatId={ChatId}", chatId);
            throw;
        }

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatContextResponse>(ct);
        return result ?? new ChatContextResponse([]);
    }

    private record SendPresetRequest(
        [property: JsonPropertyName("tenantId")]
        Guid TenantId,
        [property: JsonPropertyName("spaceId")]
        Guid SpaceId,
        [property: JsonPropertyName("botId")]
        Guid BotId,
        [property: JsonPropertyName("chatId")]
        Guid ChatId,
        [property: JsonPropertyName("funnelId")]
        Guid FunnelId,
        [property: JsonPropertyName("presetId")]
        Guid PresetId
    );
}
