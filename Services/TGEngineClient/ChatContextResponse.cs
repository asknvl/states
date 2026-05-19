using System.Text.Json.Serialization;

namespace states.Services.TgEngineService;

public record ChatContextResponse(
    [property: JsonPropertyName("messages")]
    IReadOnlyList<ChatContextMessage> Messages);

public record ChatContextMessage(
    [property: JsonPropertyName("role")]
    string Role,           // "user" | "bot"
    [property: JsonPropertyName("text")]
    string Text,
    [property: JsonPropertyName("createdAt")]
    DateTime CreatedAt);
