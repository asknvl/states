using MongoDB.Driver;
using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public class ActionTaskRepository : IActionTaskRepository
{
    private readonly IMongoCollection<ActionTaskDocument> collection;

    public ActionTaskRepository(MongoContext context)
    {
        collection = context.ActionTasks;
    }

    public async Task CreateMany(IEnumerable<ActionTaskDocument> tasks, CancellationToken ct)
    {
        var list = tasks.ToList();
        if (list.Count > 0)
            await collection.InsertManyAsync(list, cancellationToken: ct);
    }

    public async Task<ActionTaskDocument?> ClaimNext(CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending),
            Builders<ActionTaskDocument>.Filter.Lte(x => x.ScheduledAt, DateTime.UtcNow)
        );

        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.InProgress)
            .Set(x => x.ClaimedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<ActionTaskDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<ActionTaskDocument>.Sort.Ascending(x => x.ScheduledAt)
        };

        return await collection.FindOneAndUpdateAsync(filter, update, options, ct);
    }

    public async Task Complete(Guid taskId, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.Eq(x => x.Id, taskId);
        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Completed);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task Fail(Guid taskId, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.Eq(x => x.Id, taskId);
        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Failed);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task<List<ActionTaskDocument>> GetByLeadAndNode(Guid leadStateId, Guid nodeId, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.NodeId, nodeId)
        );

        return await collection.Find(filter).ToListAsync(ct);
    }

    public async Task CancelPendingByLeadAndNode(Guid leadStateId, Guid nodeId, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.NodeId, nodeId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending)
        );

        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Cancelled);

        await collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }
}
