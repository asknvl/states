namespace aiservice.Dtos.APIs.Router;

public record RouteResponseDto(List<RouteResultDto> Results);

public record RouteResultDto(string Id, bool Matched, string? Reason);
