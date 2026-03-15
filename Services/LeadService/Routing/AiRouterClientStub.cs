namespace states.Services.LeadService.Routing;

public class AiRouterClientStub : IAiRouterClient
{
    public Task<bool> CheckThesis(Guid leadStateId, string thesis, CancellationToken ct)
    {
        // TODO: реальный вызов AI сервиса с контекстом переписки лида
        return Task.FromResult(true);
    }
}
