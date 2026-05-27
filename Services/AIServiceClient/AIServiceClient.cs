using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            try
            {
                response = await http.PostAsJsonAsync("/route", request, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AIServiceClient /route request failed");
                throw;
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<RouteResponseDto>(stream, JsonOptions, ct)
                   ?? throw new InvalidOperationException("AIServiceClient /route returned null response");
        }
    }
}
