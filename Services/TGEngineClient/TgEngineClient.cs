using states.Services.TGEngineClient.Dtos;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace states.Services.TgEngineService;

public class TGEngineClient : ITGEngineClient
{
    private const string UnknownTelegramErrorCode = "UNKNOWN";
    private const string BotNotRunningCode = "BOT_NOT_RUNNING";

    private readonly HttpClient http;
    private readonly ILogger logger;

    // Таймауты пер-запросные (HttpClient.Timeout один на все вызовы и так не умеет).
    // Send-пресету нужен отдельный, больший бюджет: холодная отправка тяжёлого медиа
    // без кэша file_id (скачивание из S3 + upload в Telegram) легитимно занимает минуты.
    private readonly TimeSpan requestTimeout;
    private readonly TimeSpan sendPresetTimeout;

    public TGEngineClient(HttpClient http, IConfiguration configuration, ILogger<TGEngineClient> logger)
    {
        this.http = http;
        this.logger = logger;

        requestTimeout = TimeSpan.FromSeconds(
            configuration.GetValue("TgEngineClient:TimeoutSeconds", 30));
        sendPresetTimeout = TimeSpan.FromSeconds(
            configuration.GetValue("TgEngineClient:SendPresetTimeoutSeconds", 300));
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

        await PostAsync("/messages/send-ai-text", body, chatId, ct);
    }

    public async Task SendTyping(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        int durationMs,
        CancellationToken ct)
    {
        var body = new SendTypingDto(
            tenantId,
            spaceId,
            botId,
            chatId,
            durationMs);

        await PostAsync("/actions/typing", body, chatId, ct);
    }

    public async Task ReadChatHistory(
        Guid tenantId,
        Guid spaceId,
        Guid botId,
        Guid chatId,
        CancellationToken ct)
    {
        var body = new ReadHistoryDto(
            tenantId,
            spaceId,
            botId,
            chatId,
            MaxMessageId: null);

        await PostAsync("/actions/read", body, chatId, ct);
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

        var response = await PostAsync("/chat-ai-context", body, chatId, ct);

        var responseText = await response.Content.ReadAsStringAsync(ct);

        logger.LogInformation(
            "TgEngineClient GetContextMessages response: StatusCode={StatusCode}, Body={Body}",
            response.StatusCode,
            responseText);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            // 2xx без тела — оборванный ответ, считаем временной ошибкой
            logger.LogError(
                "TgEngineClient GetContextMessages returned success status {StatusCode} but an empty body: chatId={ChatId}",
                response.StatusCode, chatId);
            throw new TransientActionException(
                $"TgEngineClient /chat-ai-context returned an empty body with status {(int)response.StatusCode}");
        }

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

        await PostAsync("/botpresets/send", body, chatId, ct, sendPresetTimeout);
    }

    private async Task<HttpResponseMessage> PostAsync<TRequest>(
        string path,
        TRequest body,
        Guid chatId,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        HttpResponseMessage response;

        var effectiveTimeout = timeout ?? requestTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(effectiveTimeout);

        try
        {
            response = await http.PostAsJsonAsync(path, body, cts.Token);
        }
        catch (HttpRequestException ex)
        {
            // Обновление tgengine: соединение отвергнуто / имя не резолвится / разорванный коннект.
            // Запрос до сервиса не дошёл — повторить безопасно.
            logger.LogError(ex, "TgEngineClient {Path} request failed: chatId={ChatId}", path, chatId);
            throw new TransientActionException($"TgEngineClient {path} request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "TgEngineClient {Path} request timed out after {TimeoutSeconds}s: chatId={ChatId}",
                path, effectiveTimeout.TotalSeconds, chatId);
            throw new TransientActionException(
                $"TgEngineClient {path} request timed out after {effectiveTimeout.TotalSeconds:0}s", ex);
        }

        var status = (int)response.StatusCode;

        // 409 c code=BOT_NOT_RUNNING — бот ещё не поднялся (обычно гонка с деплоем tgengine).
        // Прочие 409 — настоящие конфликты, их повторять незачем.
        if (status == 409)
        {
            var (code, detail) = await ReadProblemDetails(response, "code", ct);

            if (code == BotNotRunningCode)
            {
                logger.LogWarning(
                    "TgEngineClient {Path}: bot is not running yet, chatId={ChatId}", path, chatId);

                throw new BotNotRunningException($"TgEngineClient {path}: {detail}");
            }

            logger.LogWarning(
                "TgEngineClient {Path} conflict: {Detail}, chatId={ChatId}", path, detail, chatId);
        }

        // 422 — Telegram отказал (FLOOD_WAIT, USER_IS_BLOCKED, ...). Не ретраим: повтор упрётся
        // в тот же лимит. Лид уходит на оператора, код отказа — в сообщении таски.
        if (status == 422)
        {
            var (rawCode, detail) = await ReadProblemDetails(response, "telegramErrorCode", ct);
            var code = rawCode ?? UnknownTelegramErrorCode;

            // Блокировка бота лидом — ожидаемый конец диалога, а не сбой: чинить нечего
            logger.Log(
                code == TelegramActionException.UserIsBlockedCode ? LogLevel.Warning : LogLevel.Error,
                "TgEngineClient {Path} rejected by Telegram: Code={Code}, Detail={Detail}, chatId={ChatId}",
                path, code, detail, chatId);

            throw new TelegramActionException(code, $"TgEngineClient {path} rejected by Telegram [{code}]: {detail}");
        }

        // 502/503/504 на деплое отдаёт прокси, не дотянувшись до апстрима; 5xx/408/429 самого
        // tgengine тоже имеет смысл повторить. Остальные 4xx (400/404) — ошибка запроса
        // или конфигурации, ретрай не поможет: пусть падает в Fail (+Manual для критичных тасок).
        if (status is 408 or 429 or >= 500)
        {
            var errorText = await response.Content.ReadAsStringAsync(ct);

            logger.LogWarning(
                "TgEngineClient {Path} returned {Status}: {Body}, chatId={ChatId}",
                path, status, errorText, chatId);

            throw new TransientActionException($"TgEngineClient {path} returned {status}: {errorText}");
        }

        response.EnsureSuccessStatusCode();

        return response;
    }

    // ProblemDetails от tgengine: текст в detail, машиночитаемый код — в поле codeProperty
    // (telegramErrorCode для отказов Telegram, code для прочих доменных ошибок).
    private async Task<(string? Code, string Detail)> ReadProblemDetails(
        HttpResponseMessage response,
        string codeProperty,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var code = root.TryGetProperty(codeProperty, out var codeElement)
                ? codeElement.GetString()
                : null;

            var detail = root.TryGetProperty("detail", out var detailElement)
                ? detailElement.GetString()
                : null;

            return (code, detail ?? body);
        }
        catch (JsonException)
        {
            // Тело не ProblemDetails (например, ответил прокси) — кода нет, остаётся сам статус
            return (null, body);
        }
    }
}
