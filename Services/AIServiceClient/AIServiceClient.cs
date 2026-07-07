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

        public async Task<RouteResponseDto> RouteAsync(RouteRequestDto request, CancellationToken ct)
        {
            HttpResponseMessage response;
            string responseText;

            try
            {
                response = await http.PostAsJsonAsync("/route", request, JsonOptions, ct);

                responseText = await response.Content.ReadAsStringAsync(ct);

                logger.LogInformation(
                    "AIServiceClient Route response: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode,
                    responseText);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AIServiceClient /route request failed");
                throw;
            }

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                logger.LogError(
                    "AIServiceClient /route returned success status {StatusCode} but an empty body",
                    response.StatusCode);
                throw new InvalidOperationException(
                    $"AIServiceClient /route returned an empty body with status {(int)response.StatusCode}");
            }

            return JsonSerializer.Deserialize<RouteResponseDto>(responseText, JsonOptions)
                   ?? throw new InvalidOperationException("AIServiceClient /route returned null response");
        }

        public async Task<ReplyResponseDto> ReplyAsync(ReplyRequestDto request, CancellationToken ct)
        {
            HttpResponseMessage response;
            string responseText;

            try
            {
                response = await http.PostAsJsonAsync("/reply", request, JsonOptions, ct);

                responseText = await response.Content.ReadAsStringAsync(ct);

                logger.LogInformation(
                    "AIServiceClient Reply response: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode,
                    responseText);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AIServiceClient /reply request failed");
                throw;
            }

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                logger.LogError(
                    "AIServiceClient /reply returned success status {StatusCode} but an empty body",
                    response.StatusCode);
                throw new InvalidOperationException(
                    $"AIServiceClient /reply returned an empty body with status {(int)response.StatusCode}");
            }

            return JsonSerializer.Deserialize<ReplyResponseDto>(responseText, JsonOptions)
                   ?? throw new InvalidOperationException("AIServiceClient /reply returned null response");
        }
    }
}
