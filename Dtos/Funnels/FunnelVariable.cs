namespace states.Dtos.Funnels
{
    public sealed record FunnelVariable(
        Guid Id,
        string Macros,
        string Value);
}
