using states.Services.TGEngineClient.Dtos;
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

    public async Task<List<ChatContextMessageDto>> GetContextMessages(
        Guid tenantId,
        Guid botId,
        Guid chatId,
        int lastMessagesNumber,
        bool isImageDetailed,
        CancellationToken ct)
    {
        var body = new GetContextMessagesRequestDto(
            tenantId,
            botId,
            chatId,
            lastMessagesNumber,
            isImageDetailed);

        HttpResponseMessage response;

        try
        {
            response = await http.PostAsJsonAsync("/chat-ai-context", body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TgEngineClient GetContextMessages failed: chatId={ChatId}", chatId);
            throw;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<ChatContextMessageDto>>(ct)
            ?? throw new InvalidOperationException("GetContextMessages returned null");
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
        var body = new SendPresetRequestDto(
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

   
}
