namespace states.Dtos.Funnels
{
    public sealed record SetAiPromptsRequest(
        string? GlobalLegend,
        string? Restrictions,
        string? ResponseStyle);
}
