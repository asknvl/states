using System.Text.Json.Serialization;

namespace states.Services.TGEngineClient.Dtos
{
    internal record SendPresetMessageDto(       
       Guid TenantId,       
       Guid SpaceId,       
       Guid BotId,       
       Guid ChatId,
       List<Variable> Variables,
       [property: JsonPropertyName("funnelId")]
       Guid FunnelId,
       [property: JsonPropertyName("presetId")]
       Guid PresetId) : SendMessageBaseDto(
           TenantId: TenantId,
           SpaceId: SpaceId,
           BotId: BotId,
           ChatId: ChatId,
           Variables: Variables);
}
