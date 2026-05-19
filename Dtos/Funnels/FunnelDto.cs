using states.Services.FunnelService.Application;

namespace states.Dtos.Funnels
{
    public sealed record FunnelDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string Name,
        string? Description,
        List<Tag> Tags,
        List<FlowShortDto> Flows,
        bool? IsActive,
        bool IsInputTranslatorOn,
        bool IsOutputTranslatorOn,
        RecognitionType PhotoRecognition,
        RecognitionType VideoRecognition,
        RecognitionType VoiceRecognition,
        int ReadDelay,
        int ReplyDelay
    );
}
