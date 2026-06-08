using System.Text.Json.Serialization;

namespace states.Services.TGEngineClient.Dtos
{
    internal record SendTextMessagesDto(        
        Guid TenantId,        
        Guid SpaceId,        
        Guid BotId,        
        Guid ChatId,
        List<Variable> Variables,
        [property: JsonPropertyName("text")]
        string Text) : SendMessageBaseDto(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            Variables: Variables);
}
