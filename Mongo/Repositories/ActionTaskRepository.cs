using MongoDB.Driver;
using MongoDB.Bson;
using states.Mongo.Documents;
using states.Services.FunnelService.Application;

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
        if (list.Count == 0)
            return;

        try
        {
            // IsOrdered = false: при конфликте по unique-индексу остальные таски пачки всё равно вставятся
            await collection.InsertManyAsync(list, new InsertManyOptions { IsOrdered = false }, ct);
        }
        catch (MongoBulkWriteException<ActionTaskDocument> ex)
            when (ex.WriteErrors.All(e => e.Code == 11000))
        {
            // Уже есть активный (pending/in-progress) AiReply для этого лида — дубликат не нужен
        }
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
            .Set(x => x.Status, ActionStatus.Completed)
            .Set(x => x.FinishedAt, DateTime.UtcNow);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task Fail(Guid taskId, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.Eq(x => x.Id, taskId);
        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Failed)
            .Set(x => x.FinishedAt, DateTime.UtcNow);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    // Возврат таски в очередь после transient-ошибки: снова Pending с будущим ScheduledAt.
    // Таска остаётся «активной» (unique_active_ai_reply_per_lead продолжает держать слот,
    // CancelPendingByLead при переходе лида на другую ноду отменит и её).
    public async Task Reschedule(Guid taskId, DateTime nextAttemptAt, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.Eq(x => x.Id, taskId);
        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Pending)
            .Set(x => x.ScheduledAt, nextAttemptAt)
            .Set(x => x.ClaimedAt, null)
            .Inc(x => x.Attempt, 1);

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
            .Set(x => x.Status, ActionStatus.Cancelled)
            .Set(x => x.FinishedAt, DateTime.UtcNow);

        await collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task CancelPendingByLead(Guid leadStateId, CancellationToken ct)
    {
        // MarkRead не отменяем: прочитка уже полученных сообщений валидна независимо от того,
        // на какую ноду перешёл лид (иначе переход по AiRouter гасил бы ещё не сработавшую прочитку).
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<ActionTaskDocument>.Filter.In(x => x.Status, new[] { ActionStatus.Pending, ActionStatus.Waiting }),
            Builders<ActionTaskDocument>.Filter.Ne(x => x.Type, ActionType.MarkRead)
        );

        var update = Builders<ActionTaskDocument>.Update
            .Set(x => x.Status, ActionStatus.Cancelled)
            .Set(x => x.FinishedAt, DateTime.UtcNow);

        await collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task UnlockNext(Guid leadStateId, Guid nodeId, int completedOrder, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, leadStateId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.NodeId, nodeId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Order, completedOrder + 1),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Waiting)
        );

        var update = Builders<ActionTaskDocument>.Update.Set(x => x.Status, ActionStatus.Pending);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task TryInsertAiReplyTask(AiReplyActionTaskDocument task, CancellationToken ct)
    {
        try
        {
            await collection.InsertOneAsync(task, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            // Уже есть активный (pending/in-progress) AiReply для этого лида — дубликат не нужен
        }
    }

    public async Task UpsertPendingMarkReadTask(MarkReadActionTaskDocument task, CancellationToken ct)
    {
        // Лид может прислать несколько сообщений подряд — вместо пачки тасков
        // сдвигаем время прочитки у уже ожидающего (как человек: дочитывает всё разом).
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, task.LeadStateId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.MarkRead),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending)
        );

        var taskBson = task.ToBsonDocument();
        taskBson.Remove("scheduledAt");

        var update = new BsonDocument
        {
            { "$set", new BsonDocument("scheduledAt", new BsonDateTime(task.ScheduledAt)) },
            { "$setOnInsert", taskBson }
        };

        await collection.UpdateOneAsync(
            filter,
            new BsonDocumentUpdateDefinition<ActionTaskDocument>(update),
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    public async Task UpsertPendingAiRouterTask(AiRouterActionTaskDocument task, CancellationToken ct)
    {
        var filter = Builders<ActionTaskDocument>.Filter.And(
            Builders<ActionTaskDocument>.Filter.Eq(x => x.LeadStateId, task.LeadStateId),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.AiRouter),
            Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending)
        );

        var taskBson = task.ToBsonDocument();
        taskBson.Remove("scheduledAt");

        var update = new BsonDocument
        {
            { "$set", new BsonDocument("scheduledAt", new BsonDateTime(task.ScheduledAt)) },
            { "$setOnInsert", taskBson }
        };

        await collection.UpdateOneAsync(
            filter,
            new BsonDocumentUpdateDefinition<ActionTaskDocument>(update),
            new UpdateOptions { IsUpsert = true },
            ct);
    }
}
