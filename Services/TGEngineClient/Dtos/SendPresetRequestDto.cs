using System.Text.Json.Serialization;

namespace states.Services.TGEngineClient.Dtos
{
    internal record SendPresetRequestDto(
       [property: JsonPropertyName("tenantId")]
        Guid TenantId,
       [property: JsonPropertyName("spaceId")]
        Guid SpaceId,
       [property: JsonPropertyName("botId")]
        Guid BotId,
       [property: JsonPropertyName("chatId")]
        Guid ChatId,
       [property: JsonPropertyName("funnelId")]
        Guid FunnelId,
       [property: JsonPropertyName("presetId")]
        Guid PresetId
   );
}
