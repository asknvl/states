using aiservice.Dtos.APIs.Chat;
using System.ComponentModel.DataAnnotations;

namespace aiservice.Dtos.APIs.Router;

public record RouteRequestDto(
    Guid TenantId,
    Guid ChatId,
    Guid BotId,
    Guid ModelPresetId,
    List<RouterRuleDto> Routers,
    List<ChatContextMessageDto> Context,
    RouteOptionsDto? Options
) : AiServiceBaseRequestDto(
    TenantId: TenantId,
    BotId: BotId,
    ChatId: ChatId);

public record RouterRuleDto(
    string Id,
    string Thesis
);

public record RouteOptionsDto(
    bool ReturnOnlyOne = false,
    bool IncludeReason = false
);
