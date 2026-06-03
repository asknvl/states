using System.Text.Json.Serialization;

namespace states.Services.TGEngineClient.Dtos
{
    internal record SendTextMessagesDto(
        [property: JsonPropertyName("tenantId")]
        Guid TenantId,
        [property: JsonPropertyName("spaceId")]
        Guid SpaceId,
        [property: JsonPropertyName("botId")]
        Guid BotId,
        [property: JsonPropertyName("chatId")]
        Guid ChatId,
        [property: JsonPropertyName("text")]
        string Text
    );
}
