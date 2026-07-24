using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using aiservice.Dtos.APIs.Reply;
using aiservice.Dtos.APIs.Router;

namespace states.Services.AIServiceClient
{
    public class AIServiceClient(HttpClient http, ILogger<AIServiceClient> logger) : IAIServiceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public Task<RouteResponseDto> RouteAsync(RouteRequestDto request, CancellationToken ct) =>
            PostAsync<RouteRequestDto, RouteResponseDto>("/route", request, ct);

        public Task<ReplyResponseDto> ReplyAsync(ReplyRequestDto request, CancellationToken ct) =>
            PostAsync<ReplyRequestDto, ReplyResponseDto>("/reply", request, ct);

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
        {
            HttpResponseMessage response;
            string responseText;

            try
            {
                response = await http.PostAsJsonAsync(path, request, JsonOptions, ct);

                responseText = await response.Content.ReadAsStringAsync(ct);

                logger.LogInformation(
                    "AIServiceClient {Path} response: StatusCode={StatusCode}, Body={Body}",
                    path,
                    response.StatusCode,
                    responseText);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "AIServiceClient {Path} request failed", path);
                throw new TransientActionException($"AIServiceClient {path} request failed: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "AIServiceClient {Path} request timed out", path);
                throw new TransientActionException($"AIServiceClient {path} request timed out", ex);
            }

            var status = (int)response.StatusCode;

            // Контракт aiservice: 429/502 (и любые 5xx/408) — временная ошибка, ретраим;
            // остальные 4xx (400/404/422) — ошибка конфигурации или отвергнутый запрос, ретрай не поможет.
            if (status is 408 or 429 or >= 500)
                throw new TransientActionException($"AIServiceClient {path} returned {status}: {responseText}");

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                // 2xx без тела — оборванный ответ, считаем временной ошибкой
                logger.LogError(
                    "AIServiceClient {Path} returned success status {StatusCode} but an empty body",
                    path,
                    response.StatusCode);
                throw new TransientActionException(
                    $"AIServiceClient {path} returned an empty body with status {status}");
            }

            return JsonSerializer.Deserialize<TResponse>(responseText, JsonOptions)
                   ?? throw new InvalidOperationException($"AIServiceClient {path} returned null response");
        }
    }
}
