using states.Mongo.Documents;

namespace states.Services.LeadService.Worker;

public interface IActionExecutor
{
    Task Execute(ActionTaskDocument task, CancellationToken ct);
}
