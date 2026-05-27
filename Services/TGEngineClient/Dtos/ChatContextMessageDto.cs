namespace states.Services.TGEngineClient.Dtos
{
    public sealed record ChatContextMessageDto(
        string Role,
        string? Text,
        ImageContentDto? Image);

    public record ImageContentDto(
        string MimeType,
        string Data);
}
