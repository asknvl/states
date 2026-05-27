using aiservice.Dtos.APIs.Router;

namespace states.Services.AIServiceClient
{
    public interface IAIServiceClient
    {
        Task<RouteResponseDto> RouteAsync(RouteRequestDto request, CancellationToken ct);
    }
}
