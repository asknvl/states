using MongoDB.Bson;
using MongoDB.Driver;
using states.Mongo.Documents;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Repositories;

public class LeadStateRepository : ILeadStateRepository
{
    private readonly IMongoCollection<FunnelLeadState> collection;

    public LeadStateRepository(MongoContext context)
    {
        collection = context.LeadStates;
    }

    public async Task<FunnelLeadState> CreateLeadState(FunnelLeadState state, CancellationToken ct)
    {
        await collection.InsertOneAsync(state, cancellationToken: ct);
        return state;
    }

    public async Task<FunnelLeadState> GetLeadState(Guid leadStateId, CancellationToken ct)
    {
        var state = await collection
            .Find(x => x.Id == leadStateId)
            .FirstOrDefaultAsync(ct);

        if (state is null)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

        return state;
    }

    public async Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {
        return await collection
            .Find(x => x.TenantId == tenantId && x.BotId == botId && x.ChatId == chatId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid chatId, CancellationToken ct)
    {
        return await collection
            .Find(x => x.TenantId == tenantId && x.ChatId == chatId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        // MongoDB не позволяет в одном update одновременно менять элементы массива через $[...]
        // и делать $push в тот же массив — разбиваем на два вызова.

        var closeCurrentState = Builders<FunnelLeadState>.Update
            .Set(x => x.NodeId, nextNodeId)
            .Set("statesLog.$[currentState].leftAt", now)
            .Set("statesLog.$[currentState].exitEdgeId", new BsonBinaryData(edgeId, GuidRepresentation.Standard));

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
                new BsonDocument("currentState.leftAt", BsonNull.Value))
        };

        var result = await collection.UpdateOneAsync(filter, closeCurrentState, new UpdateOptions { ArrayFilters = arrayFilters }, ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

        var pushNextState = Builders<FunnelLeadState>.Update
            .Push(x => x.StatesLog, new StateLogEntry
            {
                NodeId = nextNodeId,
                EnteredAt = now,
                ActionsLog = actions
            });

        //TODO OUTBOX

        await collection.UpdateOneAsync(filter, pushNextState, cancellationToken: ct);
    }

    public async Task UpdateActionStatus(
        Guid leadStateId,
        Guid nodeId,
        Guid actionId,
        ActionStatus status,
        CancellationToken ct,
        string? errorMessage = null)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var update = Builders<FunnelLeadState>.Update
            .Set("statesLog.$[state].actions.$[action].status", status.ToString())
            .Set("statesLog.$[state].actions.$[action].timeStamp", DateTime.UtcNow)
            .Set("statesLog.$[state].actions.$[action].errorMessage", errorMessage);

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

    public async Task UpdateLeadStateStatus(Guid leadStateId, LeadFunnelStatus status, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);
        var update = Builders<FunnelLeadState>.Update.Set(x => x.Status, status);

        //TODO OUTBOX

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");
    }

    public async Task ManageTag(Guid leadStateId, TagOperation operation, Guid tagId, Guid? replacementTagId, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        UpdateDefinition<FunnelLeadState> update = operation switch
        {
            TagOperation.Add =>
                Builders<FunnelLeadState>.Update.AddToSet(x => x.Tags, tagId),

            TagOperation.Remove =>
                Builders<FunnelLeadState>.Update.Pull(x => x.Tags, tagId),

            TagOperation.Replace when replacementTagId.HasValue =>
                Builders<FunnelLeadState>.Update
                    .Pull(x => x.Tags, tagId)
                    .AddToSet(x => x.Tags, replacementTagId.Value),

            _ => throw new InvalidOperationException($"Unsupported tag operation: {operation}")
        };

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

        //TODO OUTBOX
    }

    public async Task Delete(Guid leadStateId, CancellationToken ct)
    {
        var result = await collection.DeleteOneAsync(x => x.Id == leadStateId, ct);

        if (result.DeletedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");
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
