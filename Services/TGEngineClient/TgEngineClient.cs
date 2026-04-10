using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace states.Services.TgEngineService;

public class TgEngineClient : ITGEngineClient
{
    private readonly HttpClient http;
    private readonly ILogger logger;

    public TgEngineClient(HttpClient http, ILogger<TgEngineClient> logger)
    {
        this.http = http;
        this.logger = logger;
    }

    public async Task SendPreset(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        Guid presetId,
        CancellationToken ct)
    {
        var body = new SendPresetRequest(tenantId, spaceId, botId, chatId, presetId);

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

    private record SendPresetRequest(
        [property: JsonPropertyName("tenantId")] Guid TenantId,
        [property: JsonPropertyName("spaceId")]  Guid SpaceId,
        [property: JsonPropertyName("botId")]    Guid BotId,
        [property: JsonPropertyName("chatId")]   Guid ChatId,
        [property: JsonPropertyName("presetId")] Guid PresetId);
}
