using aiservice.Dtos.APIs.Chat;

namespace aiservice.Dtos.APIs.Reply
{
    public record ReplyRequestDto(

        Guid TenantId,
        Guid ChatId,
        Guid BotId,
        Guid ModelPresetId,

        string? GlobalLegend,
        string? Restrictions,
        string? ResponseStyle,
        
        string? Goal,
        string? Requirements,
        string? Legend,
        string? AdditionalInfo,

        double Temperature,

        List<ChatContextMessageDto> Context) : AiServiceBaseRequestDto(
            TenantId: TenantId,
            BotId: BotId,
            ChatId: ChatId
        );
    
}
