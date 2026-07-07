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

    public async Task SendAiTextMessages(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        List<Variable> variables,
        string text,
        CancellationToken ct)
    {
        var body = new SendTextMessagesDto(
            tenantId,
            spaceId,
            botId,
            chatId,
            variables,
            text);

        HttpResponseMessage response;

        try
        {
            response = await http.PostAsJsonAsync("/messages/send-ai-text", body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TgEngineClient SendAiTextMessages failed: chatId={ChatId}", chatId);
            throw;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ChatContextMessageDto>> GetContextMessages(
        Guid tenantId,
        Guid botId,
        Guid chatId,
        int lastMessagesNumber,
        bool returnFromLastOutcoming,
        bool isImageDetailed,
        CancellationToken ct)
    {
        var body = new GetContextMessagesRequestDto(
            tenantId,
            botId,
            chatId,
            lastMessagesNumber,
            returnFromLastOutcoming,
            isImageDetailed);

        HttpResponseMessage response;

        try
        {
            response = await http.PostAsJsonAsync("/chat-ai-context", body, ct);

            var responseText = await response.Content.ReadAsStringAsync(ct);

            logger.LogInformation(
                "TgEngineClient GetContextMessages response: StatusCode={StatusCode}, Body={Body}",
                response.StatusCode,
                responseText);

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
        List<Variable> variables,
        Guid funnelId,
        Guid presetId,    
        bool needPin,
        CancellationToken ct)
    {
        var body = new SendPresetMessageDto(
            tenantId,
            spaceId,
            botId,
            chatId,        
            variables,
            funnelId,            
            presetId,
            needPin);

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
