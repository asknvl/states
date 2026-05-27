using System.ComponentModel.DataAnnotations;

namespace aiservice.Dtos.APIs.Chat
{
    public sealed record ChatContextMessageDto(
        string Role,
        string? Text,
        ImageContentDto? image);

    public record ImageContentDto(
        string MimeType,
        string Data);

}
