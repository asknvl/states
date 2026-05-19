using states.Services.FunnelService.Application;

namespace states.Dtos.Funnels
{
    public sealed record SetRecognitionRequest(
        RecognitionType? PhotoRecognition,
        RecognitionType? VideoRecognition,
        RecognitionType? VoiceRecognition
    );
}
