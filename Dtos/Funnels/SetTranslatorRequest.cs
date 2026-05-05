namespace states.Dtos.Funnels
{
    public sealed record SetTranslatorRequest(
        bool? IsInputTranslatorOn,
        bool? IsOutputTranslatorOn
    );
}
