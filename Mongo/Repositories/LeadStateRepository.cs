using MongoDB.Bson;
using MongoDB.Driver;
using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Mongo.Documents.Outbox;
using states.Mongo.Mappers;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Repositories;

public class LeadStateRepository : ILeadStateRepository
{
    private readonly IMongoCollection<FunnelLeadState> collection;
    private readonly IMongoCollection<OutboxDocument> outbox;

    public LeadStateRepository(MongoContext context)
    {
        collection = context.LeadStates;
        outbox = context.Outbox;
    }

    #region reads
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

    public async Task<FunnelLeadState?> ClaimWaitingLeadByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.BotId, botId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.Status, LeadFunnelStatus.Waiting)
        );

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Status, LeadFunnelStatus.Nothing);

        // ReturnDocument.Before — возвращаем документ до обновления,
        // чтобы проверить actions прямо в памяти без лишнего запроса.
        // Если другой воркер уже сменил статус — filter не совпадёт и вернётся null.
        return await collection.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.Before }, ct);
    }

    public async Task<FunnelLeadState?> GetLeadStateByLeadId(Guid tenantId, string leadId, CancellationToken ct)
    {
        return await collection
            .Find(x => x.TenantId == tenantId && x.LeadId == leadId)
            .FirstOrDefaultAsync(ct);
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
    #endregion

    #region writes
    public async Task<FunnelLeadState> CreateLeadState(FunnelLeadState state, CancellationToken ct)
    {
        await InTransaction(async session =>
        {
            await collection.InsertOneAsync(session, state, cancellationToken: ct);
            await outbox.InsertOneAsync(session, new LeadStateCreatedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = state.TenantId,
                SpaceId = state.SpaceId,
                BotId = state.BotId,
                ChatId = state.ChatId,
                LeadId = state.LeadId,
                Version = state.Version,
                CampaignId = state.CampaignId,
                CampaignName = state.CampaignName,
                SourceId = state.SourceId,
                SourceName = state.SourceName,
                FunnelId = state.FunnelId,
                FunnelName = state.FunnelName,
                FlowId = state.FlowId,
                FlowName = state.FlowName,
                NodeId = state.NodeId,
                NodeLabel = state.NodeLabel,
                Status = state.Status,
                IsInputTranslatorOn = state.IsInputTranslatorOn,
                IsOutputTranslatorOn = state.IsOutputTranslatorOn,
                PhotoRecognition = state.PhotoRecognition,
                VideoRecognition = state.VideoRecognition,
                VoiceRecognition = state.VoiceRecognition
            }, cancellationToken: ct);
        }, ct);

        return state;
    }

    public async Task UpdateLeadStateStatus(Guid leadStateId, LeadFunnelStatus status, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);
        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Status, status)
            .Inc(x => x.Version, 1);

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                throw new KeyNotFoundException($"Lead state Id={leadStateId} not found.");

            await outbox.InsertOneAsync(session, new LeadStatusChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                Status = status
            }, cancellationToken: ct);
        }, ct);
    }

    #region for chats
    public async Task UpdateLeadStateStatusByChatId(Guid chatId, LeadFunnelStatus status, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId);
        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Status, status)
            .Inc(x => x.Version, 1);

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                throw new KeyNotFoundException($"Lead state chatId={chatId} not found.");

            await outbox.InsertOneAsync(session, new LeadStatusChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                Status = status
            }, cancellationToken: ct);
        }, ct);
    }
    #endregion

    public async Task SetLeadFunnelPosition(
        Guid leadStateId,
        Guid funnelId,
        string funnelName,
        Guid flowId,
        string flowName,
        Guid nodeId,
        string nodeLabel,
        LeadFunnelStatus status,
        List<ActionStatusEntry> actions,
        Guid? exitEdgeId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var closeCurrentState = Builders<FunnelLeadState>.Update
            .Set(x => x.FunnelId, funnelId)
            .Set(x => x.FunnelName, funnelName)
            .Set(x => x.FlowId, flowId)
            .Set(x => x.FlowName, flowName)
            .Set(x => x.NodeId, nodeId)
            .Set(x => x.Status, status)
            .Set(x => x.NodeLabel, nodeLabel)
            .Set("statesLog.$[currentState].leftAt", now)
            .Set("statesLog.$[currentState].exitEdgeId", exitEdgeId);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
                new BsonDocument("currentState.leftAt", BsonNull.Value))
        };

        var pushNextState = Builders<FunnelLeadState>.Update
            .Push(x => x.StatesLog, new StateLogEntry
            {
                NodeId = nodeId,
                EnteredAt = now,
                ActionsLog = actions
            })
            .Inc(x => x.Version, 1);

        await InTransaction(async session =>
        {
            var result = await collection.UpdateOneAsync(
                session, filter, closeCurrentState,
                new UpdateOptions { ArrayFilters = arrayFilters },
                ct);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, pushNextState,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            await outbox.InsertOneAsync(session, new LeadFunnelPositionChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated!.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                FunnelId = funnelId,
                FunnelName = funnelName,
                FlowId = flowId,
                FlowName = flowName,
                NodeId = nodeId,
                NodeLabel = nodeLabel,
                Status = status

            }, cancellationToken: ct);
        }, ct);
    }

    //public async Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct)
    //{
    //    var now = DateTime.UtcNow;
    //    var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

    //    var closeCurrentState = Builders<FunnelLeadState>.Update
    //        .Set(x => x.NodeId, nextNodeId)
    //        .Set("statesLog.$[currentState].leftAt", now)
    //        .Set("statesLog.$[currentState].exitEdgeId", new BsonBinaryData(edgeId, GuidRepresentation.Standard));

    //    var arrayFilters = new List<ArrayFilterDefinition>
    //    {
    //        new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
    //            new BsonDocument("currentState.leftAt", BsonNull.Value))
    //    };

    //    var pushNextState = Builders<FunnelLeadState>.Update
    //        .Push(x => x.StatesLog, new StateLogEntry
    //        {
    //            NodeId = nextNodeId,
    //            EnteredAt = now,
    //            ActionsLog = actions
    //        })
    //        .Inc(x => x.Version, 1);

    //    await InTransaction(async session =>
    //    {
    //        var result = await collection.UpdateOneAsync(
    //            session, filter, closeCurrentState,
    //            new UpdateOptions { ArrayFilters = arrayFilters },
    //            ct);

    //        if (result.MatchedCount == 0)
    //            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

    //        var updated = await collection.FindOneAndUpdateAsync(
    //            session, filter, pushNextState,
    //            new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
    //            ct);

    //        await outbox.InsertOneAsync(session, new LeadFunnelPositionChangedOutboxDocument
    //        {
    //            Id = Guid.CreateVersion7(),
    //            CreatedAt = DateTime.UtcNow,
    //            TenantId = updated!.TenantId,
    //            SpaceId = updated.SpaceId,
    //            BotId = updated.BotId,
    //            ChatId = updated.ChatId,
    //            LeadId = updated.LeadId,
    //            Version = updated.Version,
    //            FunnelId = updated.FunnelId,
    //            FunnelName = updated.FunnelName,
    //            FlowId = updated.FlowId,
    //            FlowName = updated.FlowName,
    //            NodeId = updated.NodeId,
    //            NodeLabel = updated.NodeLabel,                
    //            Status = updated.Status

    //        }, cancellationToken: ct);
    //    }, ct);
    //}

    public async Task UpdateTag(
        Guid leadStateId,
        TagOperation operation,
        Dtos.Funnels.Tag tag,
        Dtos.Funnels.Tag? replacementTag,
        CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var tagDocument = new TagDocument()
        {
            Id = tag.Id,
            Name = tag.Name
        };

        var replacementTagDocument = (replacementTag is not null) ? new TagDocument()
        {
            Id = replacementTag.Id,
            Name = replacementTag.Name
        } : null;

        UpdateDefinition<FunnelLeadState> update = operation switch
        {
            TagOperation.Add =>
                Builders<FunnelLeadState>.Update.AddToSet(x => x.Tags, tagDocument),

            TagOperation.Remove =>
                Builders<FunnelLeadState>.Update.Pull(x => x.Tags, tagDocument),

            TagOperation.Replace when replacementTag is not null =>
                Builders<FunnelLeadState>.Update
                    .Pull(x => x.Tags, tagDocument)
                    .AddToSet(x => x.Tags, replacementTagDocument),

            _ => throw new InvalidOperationException($"Unsupported tag operation: {operation}")
        };

        update = Builders<FunnelLeadState>.Update.Combine(update, Builders<FunnelLeadState>.Update.Inc(x => x.Version, 1));

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

            await outbox.InsertOneAsync(session, new LeadTagChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                //FunnelId = updated.FunnelId,
                Tags = updated.Tags,
                Operation = operation,
                Tag = tagDocument,
            }, cancellationToken: ct);
        }, ct);
    }

    public async Task SaveTags(Guid leadStateId, List<Dtos.Funnels.Tag> tags, CancellationToken ct)
    {
        
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);
        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Tags, tags.Select(t => t.ToDocument()))
            .Inc(x => x.Version, 1);

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

            await outbox.InsertOneAsync(session, new LeadTagChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                //FunnelId = updated.FunnelId,
                Tags = updated.Tags ?? [],
                Operation = TagOperation.Manual,
                Tag = null
            }, cancellationToken: ct);
        }, ct);
    }

    public async Task SetIsTranslatorOn(Guid leadStateId, bool? isInputTranslatorOn, bool? isOutputTranslatorOn, CancellationToken ct)
    {
        if (isInputTranslatorOn is null && isOutputTranslatorOn is null)
            return;

        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var updates = new List<UpdateDefinition<FunnelLeadState>>
        {
            Builders<FunnelLeadState>.Update.Inc(x => x.Version, 1)
        };
        if (isInputTranslatorOn.HasValue)
            updates.Add(Builders<FunnelLeadState>.Update.Set(x => x.IsInputTranslatorOn, isInputTranslatorOn.Value));
        if (isOutputTranslatorOn.HasValue)
            updates.Add(Builders<FunnelLeadState>.Update.Set(x => x.IsOutputTranslatorOn, isOutputTranslatorOn.Value));

        var update = Builders<FunnelLeadState>.Update.Combine(updates);

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null) throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");

            await outbox.InsertOneAsync(session, new LeadTranslatorChangedOutboxDocument
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                TenantId = updated.TenantId,
                SpaceId = updated.SpaceId,
                BotId = updated.BotId,
                ChatId = updated.ChatId,
                LeadId = updated.LeadId,
                Version = updated.Version,
                IsInputTranslatorOn = updated.IsInputTranslatorOn,
                IsOutputTranslatorOn = updated.IsOutputTranslatorOn
            }, cancellationToken: ct);
        }, ct);
    }

    public async Task UpdateActionStatus(Guid leadStateId, Guid nodeId, Guid actionId, ActionStatus status, CancellationToken ct, string? errorMessage = null)
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

    public async Task Delete(Guid leadStateId, CancellationToken ct)
    {
        var result = await collection.DeleteOneAsync(x => x.Id == leadStateId, ct);

        if (result.DeletedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");
    }
    #endregion

    #region private
    private async Task InTransaction(Func<IClientSessionHandle, Task> action, CancellationToken ct)
    {
        using var session = await collection.Database.Client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();
        try
        {
            await action(session);
            await session.CommitTransactionAsync(ct);
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
    #endregion
}
