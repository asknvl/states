using MongoDB.Driver;
using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public class PushTaskRepository : IPushTaskRepository
{
    private readonly IMongoCollection<PushTaskDocument> collection;

    public PushTaskRepository(MongoContext context)
    {
        collection = context.PushTasks;
    }

    public async Task CreateMany(IEnumerable<PushTaskDocument> tasks, CancellationToken ct)
    {
        var list = tasks.ToList();
        if (list.Count > 0)
            await collection.InsertManyAsync(list, cancellationToken: ct);
    }

    public async Task<PushTaskDocument?> ClaimNext(CancellationToken ct)
    {
        var filter = Builders<PushTaskDocument>.Filter.And(
            Builders<PushTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending),
            Builders<PushTaskDocument>.Filter.Lte(x => x.ScheduledAt, DateTime.UtcNow)
        );

        var update = Builders<PushTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.InProgress)
            .Set(x => x.ClaimedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<PushTaskDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<PushTaskDocument>.Sort.Ascending(x => x.ScheduledAt)
        };

        return await collection.FindOneAndUpdateAsync(filter, update, options, ct);
    }

    // Завершённые/проваленные/отменённые push-таски не нужны после обработки — память об успешной
    // отправке хранится в FunnelLeadState.Pushes, поэтому документ просто удаляется, а не маркируется.
    public async Task Complete(Guid taskId, CancellationToken ct)
    {
        await collection.DeleteOneAsync(x => x.Id == taskId, ct);
    }

    public async Task Fail(Guid taskId, CancellationToken ct)
    {
        await collection.DeleteOneAsync(x => x.Id == taskId, ct);
    }

    public async Task CancelPendingByLead(Guid leadStateId, CancellationToken ct)
    {
        var filter = Builders<PushTaskDocument>.Filter.And(
            Builders<PushTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<PushTaskDocument>.Filter.In(x => x.Status, new[] { ActionStatus.Pending, ActionStatus.Waiting })
        );

        await collection.DeleteManyAsync(filter, ct);
    }

    public async Task UnlockNext(Guid leadStateId, Guid nodeId, int completedOrder, CancellationToken ct)
    {
        var filter = Builders<PushTaskDocument>.Filter.And(
            Builders<PushTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<PushTaskDocument>.Filter.Eq(x => x.NodeId, nodeId),
            Builders<PushTaskDocument>.Filter.Eq(x => x.Order, completedOrder + 1),
            Builders<PushTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Waiting)
        );

        var update = Builders<PushTaskDocument>.Update.Set(x => x.Status, ActionStatus.Pending);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}
