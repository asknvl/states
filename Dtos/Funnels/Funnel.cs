using states.Mongo.Documents;
using states.Services.FunnelService.Application;

namespace states.Dtos.Funnels
{
    public sealed record Funnel(
            Guid Id,
            Guid TenantId,
            Guid SpaceId,
            string Name,
            string? Description,
            List<Tag> Tags,
            List<FunnelVariable> Variables,
            List<Flow> Flows,
            Guid PresetsFolderId,
            bool? IsActive,
            bool IsInputTranslatorOn,
            bool IsOutputTranslatorOn,
            RecognitionType PhotoRecognition,
            RecognitionType VideoRecognition,
            RecognitionType VoiceRecognition,
            int ReadDelay,
            int ReplyDelay,
            Guid AiRouterModelPresetId,
            Guid AiReplyModelPresetId,
            double AiReplyTemperature,
            string? GlobalLegend,
            string? Restrictions,
            string? ResponseStyle
        );
}
