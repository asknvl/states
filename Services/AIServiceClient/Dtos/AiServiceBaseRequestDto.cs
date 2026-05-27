namespace aiservice.Dtos.APIs
{
    public record AiServiceBaseRequestDto(
        Guid TenantId,
        Guid ChatId,
        Guid BotId);
}
