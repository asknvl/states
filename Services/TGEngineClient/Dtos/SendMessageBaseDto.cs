using System.Text.Json.Serialization;

namespace states.Services.TGEngineClient.Dtos
{
    public record SendMessageBaseDto(
       [property: JsonPropertyName("tenantId")]
       Guid TenantId,
       [property: JsonPropertyName("spaceId")]
       Guid SpaceId,
       [property: JsonPropertyName("botId")]
       Guid BotId,
       [property: JsonPropertyName("chatId")]
       Guid ChatId,
       [property: JsonPropertyName("variables")]
       List<Variable> Variables);


    public record Variable(
        [property: JsonPropertyName("macros")]
        string Macros,
        [property: JsonPropertyName("value")]
        string Value);
}
