namespace states.Dtos.Funnels
{
    public sealed record SetDelaysRequest(
        int? ReadDelay,
        int? ReplyDelay
    );
}
