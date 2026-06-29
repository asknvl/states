using states.Mongo.Documents;

namespace states.Services.LeadService.Worker;

public interface IPushExecutor
{
    Task Execute(PushTaskDocument task, CancellationToken ct);
}
