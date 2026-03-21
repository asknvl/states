using MongoDB.Bson;
using MongoDB.Driver;
using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public class LeadStateRepository : ILeadStateRepository
{
    private readonly IMongoCollection<FunnelLeadState> collection;

    public LeadStateRepository(MongoContext context)
    {
        collection = context.LeadStates;
    }

    public async Task<FunnelLeadState> Create(FunnelLeadState state, CancellationToken ct)
    {
        await collection.InsertOneAsync(state, cancellationToken: ct);
        return state;
    }

    public async Task<FunnelLeadState> Get(Guid leadStateId, CancellationToken ct)
    {
        var state = await collection
            .Find(x => x.Id == leadStateId)
            .FirstOrDefaultAsync(ct);

        if (state is null)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

        return state;
    }

    public async Task<FunnelLeadState?> GetByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {
        return await collection
            .Find(x => x.TenantId == tenantId && x.BotId == botId && x.ChatId == chatId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.NodeId, nextNodeId)
            .Set(x => x.StatesLog[-1].LeftAt, now)
            .Set(x => x.StatesLog[-1].ExitEdgeId, edgeId)
            .Push(x => x.StatesLog, new StateLogEntry
            {
                NodeId = nextNodeId,
                EnteredAt = now,
                ActionsLog = actions
            });

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");
    }

    public async Task UpdateActionStatus(Guid leadStateId, Guid nodeId, Guid actionId, ActionStatus status, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var update = Builders<FunnelLeadState>.Update
            .Set("statesLog.$[state].actions.$[action].status", status)
            .Set("statesLog.$[state].actions.$[action].timeStamp", DateTime.UtcNow);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
                new BsonDocument
                {
                    { "state.nodeId", new BsonBinaryData(nodeId, GuidRepresentation.Standard) },
                    { "state.leftAt", BsonNull.Value }
                }),
            new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
                new BsonDocument
                {
                    { "action.actionId", new BsonBinaryData(actionId, GuidRepresentation.Standard) }
                })
        };

        await collection.UpdateOneAsync(filter, update, new UpdateOptions { ArrayFilters = arrayFilters }, ct);
    }

    public async Task<bool> AreAllActionsCompleted(Guid leadStateId, Guid nodeId, CancellationToken ct)
    {
        var state = await collection
            .Find(x => x.Id == leadStateId)
            .FirstOrDefaultAsync(ct);

        if (state is null) return false;

        var currentLog = state.StatesLog.LastOrDefault(s => s.NodeId == nodeId && s.LeftAt == null);
        if (currentLog is null) return false;
        if (currentLog.ActionsLog.Count == 0) return true;

        return currentLog.ActionsLog.All(a => a.Status == ActionStatus.Completed);
    }
}
