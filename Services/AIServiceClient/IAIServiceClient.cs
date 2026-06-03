using aiservice.Dtos.APIs.Reply;
using aiservice.Dtos.APIs.Router;

namespace states.Services.AIServiceClient
{
    public interface IAIServiceClient
    {
        Task<RouteResponseDto> RouteAsync(RouteRequestDto request, CancellationToken ct);
        Task<ReplyResponseDto> ReplyAsync(ReplyRequestDto request, CancellationToken ct);
    }
}
