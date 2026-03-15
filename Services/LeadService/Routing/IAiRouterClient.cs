namespace states.Services.LeadService.Routing;

public interface IAiRouterClient
{
    Task<bool> CheckThesis(Guid leadStateId, string thesis, CancellationToken ct);
}
