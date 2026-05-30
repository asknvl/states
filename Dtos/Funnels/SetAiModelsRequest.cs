namespace states.Dtos.Funnels
{
    public sealed record SetAiModelsRequest(
        Guid? AiRouterModelPresetId,
        Guid? AiReplyModelPresetId,
        double? AiReplyTemperature);
}
