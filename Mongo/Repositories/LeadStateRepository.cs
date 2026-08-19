using MongoDB.Bson;
using MongoDB.Driver;
using states.Dtos.Funnels;
using states.Mongo.Documents;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Documents.Outbox;
using states.Mongo.Mappers;
using states.Services.CampaignClient;
using states.Services.FunnelService.Application;
using states.Services.LeadEventsService.Application;
using states.Services.LeadService;

namespace states.Mongo.Repositories;

public class LeadStateRepository : ILeadStateRepository
{
    private readonly IMongoCollection<FunnelLeadState> collection;
    private readonly IMongoCollection<OutboxDocument> outbox;
    private readonly IMongoCollection<LeadEventBaseDocument> leadEvents;
    private readonly IMongoCollection<ActionTaskDocument> actionTasks;
    private readonly IMongoCollection<PushTaskDocument> pushTasks;

    public LeadStateRepository(MongoContext context, ILogger<LeadStateRepository> logger)
    {
        collection = context.LeadStates;
        outbox = context.Outbox;
        leadEvents = context.LeadEvents;
        actionTasks = context.ActionTasks;
        pushTasks = context.PushTasks;
        this.logger = logger;
    }

    private readonly ILogger<LeadStateRepository> logger;

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

    // Один leadId может встречаться в нескольких FunnelLeadState — кампания может вести лида
    // через несколько ботов одновременно, и у каждого бота свой документ состояния.
    // Сортировка по createdAt обязательна: вызывающие (постбэки, AutoActions) работают с [0]
    // как с самым первым состоянием лида, а без сортировки порядок выдачи Mongo не определён.
    // Tie-break по Id (Guid v7, монотонный по времени) — на случай одинаковых createdAt.
    public async Task<List<FunnelLeadState>> GetLeadStatesByLeadId(Guid tenantId, string leadId, CancellationToken ct)
    {
        return await collection
            .Find(x => x.TenantId == tenantId && x.LeadId == leadId)
            .Sort(Builders<FunnelLeadState>.Sort
                .Ascending(x => x.CreatedAt)
                .Ascending(x => x.Id))
            .ToListAsync(ct);
    }

    public async Task<bool> AreAllActionsFinished(Guid leadStateId, Guid nodeId, CancellationToken ct)
    {
        var state = await collection
            .Find(x => x.Id == leadStateId)
            .FirstOrDefaultAsync(ct);

        if (state is null) return false;

        var currentLog = state.StatesLog.LastOrDefault(s => s.NodeId == nodeId && s.LeftAt == null);
        if (currentLog is null) return false;
        if (currentLog.ActionsLog.Count == 0) return true;

        // Failed/Cancelled — тоже терминальные: упавший некритичный экшен не должен навсегда
        // держать ноду незавершённой, иначе лид замирает в воронке (инцидент 2026-08-19).
        return currentLog.ActionsLog.All(a =>
            a.Status is ActionStatus.Completed or ActionStatus.Failed or ActionStatus.Cancelled);
    }
    #endregion

    #region writes
    public async Task<FunnelLeadState> CreateLeadState(FunnelLeadState state, CancellationToken ct)
    {
        await InTransaction(async session =>
        {
            // Депозиты и postback-параметры относятся к лиду, а не к боту, поэтому новый документ
            // не стартует с нуля: депозитные агрегаты пересчитываем из leadEvents (источник истины
            // по депозитам), параметры копируем из уже существующего состояния этого лида.
            var deposits = await ComputeDepositAggregates(session, state.TenantId, state.LeadId, ct);
            if (deposits is not null)
            {
                state.TotalDepositAmount = deposits.Total;
                state.FirstDepositAmount = deposits.First;
                state.LastDepositAmount = deposits.Last;
                state.DepositCount = deposits.Count;
                state.CurrencyCode = deposits.CurrencyCode;
            }

            var sibling = await collection
                .Find(session, x => x.TenantId == state.TenantId && x.LeadId == state.LeadId)
                .FirstOrDefaultAsync(ct);

            if (sibling is not null)
                state.PostbackParameters = new Dictionary<string, string>(sibling.PostbackParameters);

            // Лид может входить в воронку сразу с тегами (перенесены миграцией из внешнего
            // сервиса). Событие тегов идёт следом за созданием, и его версия должна быть строго
            // выше: tgengine применяет изменение только при version < payload.Version, иначе
            // молча отбрасывает. Документ сохраняется с версией последнего отправленного события.
            var createdVersion = state.Version;

            if (state.Tags is { Count: > 0 })
                state.Version = createdVersion + 1;

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
                Version = createdVersion,
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

            // LeadStateCreated тегов не несёт, поэтому tgengine узнаёт о них тем же событием,
            // что и при обычной смене тегов, — в одной транзакции с созданием состояния.
            if (state.Tags is { Count: > 0 })
                await outbox.InsertOneAsync(session, new LeadTagChangedOutboxDocument
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTime.UtcNow,
                    TenantId = state.TenantId,
                    SpaceId = state.SpaceId,
                    BotId = state.BotId,
                    ChatId = state.ChatId,
                    LeadId = state.LeadId,
                    Version = state.Version,
                    Tags = state.Tags,
                    Operation = TagOperation.Manual,
                    Tag = null
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

    // Waiting пишется обработчиком входящих сообщений уже после захвата лида (Claim), и эта запись
    // гоняется с ExecuteTransition из воркера: если воркер успел перевести лида на следующую ноду,
    // безусловный Set(Waiting) затёр бы FinishStatus новой ноды и заблокировал автопереход
    // (guard Status == Nothing в ActionWorkerService). Поэтому пишем только если лид всё ещё
    // на той же ноде со статусом Nothing — иначе молча выходим.
    public async Task TrySetWaitingIfStillOnNode(Guid leadStateId, Guid nodeId, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.NodeId, nodeId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.Status, LeadFunnelStatus.Nothing));

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Status, LeadFunnelStatus.Waiting)
            .Inc(x => x.Version, 1);

        await InTransaction(async session =>
        {
            var updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                return;

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
                Status = updated.Status
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

    // Сохраняем текущий статус в preBlockStatus перед тем, как затереть его на Blocked,
    // чтобы при разблокировке можно было вернуть лида ровно туда, где он был.
    // Если лид уже Blocked — ничего не делаем, чтобы не затереть уже сохранённый preBlockStatus.
    public async Task<FunnelLeadState?> TryMarkFirstContact(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.BotId, botId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.FirstContactAt, null));

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.FirstContactAt, DateTime.UtcNow);

        // Если контакт уже отмечен (или лид не найден) — filter не совпадёт и вернётся null,
        // так что событие Contact пишется ровно один раз даже при конкурентных сигналах.
        return await collection.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After }, ct);
    }

    public async Task SetLastIncomingAt(Guid tenantId, Guid botId, Guid chatId, DateTime at, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.BotId, botId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId));

        var update = Builders<FunnelLeadState>.Update
            .Max(x => x.LastIncomingAt, at);

        // $max вместо $set — при переупорядоченной обработке конкурентных сигналов
        // дата не откатится назад.
        await MongoHelpers.RetryOnConnectionLoss(() => collection.UpdateManyAsync(filter, update, cancellationToken: ct), logger);
    }

    public async Task<FunnelLeadState?> MarkBlockedByChatId(Guid chatId, CancellationToken ct)
    {
        var existing = await collection.Find(x => x.ChatId == chatId).FirstOrDefaultAsync(ct);
        if (existing is null || existing.Status == LeadFunnelStatus.Blocked)
            return null;

        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId),
            Builders<FunnelLeadState>.Filter.Ne(x => x.Status, LeadFunnelStatus.Blocked));

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.PreBlockStatus, existing.Status)
            .Set(x => x.Status, LeadFunnelStatus.Blocked)
            .Inc(x => x.Version, 1);

        FunnelLeadState? updated = null;

        await InTransaction(async session =>
        {
            updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                return;

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
                Status = updated.Status
            }, cancellationToken: ct);
        }, ct);

        return updated;
    }

    // Возвращает лида в статус, в котором он был до блокировки (preBlockStatus).
    // Если preBlockStatus не сохранён (документ заблокирован до появления этого поля) — откатываемся на Waiting.
    public async Task<FunnelLeadState?> UnblockByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct)
    {
        var existing = await collection
            .Find(x => x.TenantId == tenantId && x.BotId == botId && x.ChatId == chatId)
            .FirstOrDefaultAsync(ct);

        if (existing is null || existing.Status != LeadFunnelStatus.Blocked)
            return null;

        var restoredStatus = existing.PreBlockStatus ?? LeadFunnelStatus.Waiting;

        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.BotId, botId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.ChatId, chatId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.Status, LeadFunnelStatus.Blocked));

        var update = Builders<FunnelLeadState>.Update
            .Set(x => x.Status, restoredStatus)
            .Set(x => x.PreBlockStatus, (LeadFunnelStatus?)null)
            .Inc(x => x.Version, 1);

        FunnelLeadState? updated = null;

        await InTransaction(async session =>
        {
            updated = await collection.FindOneAndUpdateAsync(
                session, filter, update,
                new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                return;

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
                Status = updated.Status
            }, cancellationToken: ct);
        }, ct);

        return updated;
    }

    // Перезаписывает actions у текущей (открытой, leftAt == null) ноды в statesLog.
    // Используется при разблокировке лида: старые action tasks были отменены вместе с блокировкой,
    // и их нужно пересоздать заново для ноды, на которой лид остановился.
    public async Task ResetCurrentNodeActions(Guid leadStateId, List<ActionStatusEntry> actions, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);

        var update = Builders<FunnelLeadState>.Update
            .Set("statesLog.$[currentState].actions", actions)
            .Inc(x => x.Version, 1);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<FunnelLeadState>(
                new BsonDocument("currentState.leftAt", BsonNull.Value))
        };

        await collection.UpdateOneAsync(filter, update, new UpdateOptions { ArrayFilters = arrayFilters }, ct);
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
        bool preserveBlocked,
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
            .Set(x => x.NodeLabel, nodeLabel)
            .Set("statesLog.$[currentState].leftAt", now)
            .Set("statesLog.$[currentState].exitEdgeId", exitEdgeId);

        // Заблокированный лид остаётся Blocked при перемещении по воронке (постбэком): целевой
        // статус кладём в PreBlockStatus, чтобы UnblockByChatId восстановил именно его после
        // того, как лид разблокирует бота. Иначе пишем статус напрямую (обычный переход).
        closeCurrentState = preserveBlocked
            ? closeCurrentState.Set(x => x.PreBlockStatus, status)
            : closeCurrentState.Set(x => x.Status, status);

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

    // Постбеки от трекера дополняют postbackParameters, а не заменяют его целиком: значения из
    // нового постбека перекрывают совпадающие ключи, остальные ключи сохраняются как есть.
    // Применяется ко всем FunnelLeadState с данным leadId — кампания может вести лида через
    // несколько ботов одновременно, и параметры относятся к лиду, а не к конкретному боту.
    public async Task MergePostbackParameters(Guid tenantId, string leadId, Dictionary<string, string> parameters, CancellationToken ct)
    {
        if (parameters.Count == 0)
            return;

        await InTransaction(async session =>
        {
            var states = await collection
                .Find(session, x => x.TenantId == tenantId && x.LeadId == leadId)
                .ToListAsync(ct);

            foreach (var state in states)
            {
                var merged = new Dictionary<string, string>(state.PostbackParameters);
                foreach (var (key, value) in parameters)
                    merged[key] = value;

                var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, state.Id);
                var update = Builders<FunnelLeadState>.Update
                    .Set(x => x.PostbackParameters, merged)
                    .Inc(x => x.Version, 1);

                var updated = await collection.FindOneAndUpdateAsync(
                    session, filter, update,
                    new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                    ct);

                if (updated is null)
                    continue;

                await outbox.InsertOneAsync(session, new LeadPostbackParametersChangedOutboxDocument
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTime.UtcNow,
                    TenantId = updated.TenantId,
                    SpaceId = updated.SpaceId,
                    BotId = updated.BotId,
                    ChatId = updated.ChatId,
                    LeadId = updated.LeadId,
                    Version = updated.Version,
                    PostbackParameters = updated.PostbackParameters
                }, cancellationToken: ct);
            }
        }, ct);
    }

    // Депозитные агрегаты — проекция из leadEvents (SALE/RESALE): не инкрементим счётчики,
    // а пересчитываем их из лога событий и проставляем одинаковые значения во все
    // FunnelLeadState лида (депозит относится к лиду, а не к конкретному боту).
    // Заодно это чинит уже разъехавшиеся документы при следующем депозите.
    // Обновляем состояния по одному (а не UpdateMany), чтобы на каждое выставить свою version
    // и записать в ту же транзакцию LeadDepositChangedOutboxDocument с его botId/chatId — по
    // аналогии с MergePostbackParameters. Событие несёт полный снапшот агрегатов, поэтому веер
    // одинаковых значений по чатам лида безопасен: потребитель делает last-write-wins upsert.
    public async Task<int?> RecalculateDeposits(Guid tenantId, string leadId, CancellationToken ct)
    {
        return await InTransaction(async session =>
        {
            var deposits = await ComputeDepositAggregates(session, tenantId, leadId, ct);
            if (deposits is null)
                return (int?)null;

            var states = await collection
                .Find(session, x => x.TenantId == tenantId && x.LeadId == leadId)
                .ToListAsync(ct);

            foreach (var state in states)
            {
                var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, state.Id);
                var update = Builders<FunnelLeadState>.Update
                    .Set(x => x.TotalDepositAmount, deposits.Total)
                    .Set(x => x.FirstDepositAmount, deposits.First)
                    .Set(x => x.LastDepositAmount, deposits.Last)
                    .Set(x => x.DepositCount, deposits.Count)
                    .Set(x => x.CurrencyCode, deposits.CurrencyCode)
                    .Inc(x => x.Version, 1);

                var updated = await collection.FindOneAndUpdateAsync(
                    session, filter, update,
                    new FindOneAndUpdateOptions<FunnelLeadState> { ReturnDocument = ReturnDocument.After },
                    ct);

                if (updated is null)
                    continue;

                await outbox.InsertOneAsync(session, new LeadDepositChangedOutboxDocument
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTime.UtcNow,
                    TenantId = updated.TenantId,
                    SpaceId = updated.SpaceId,
                    BotId = updated.BotId,
                    ChatId = updated.ChatId,
                    LeadId = updated.LeadId,
                    Version = updated.Version,
                    TotalDepositAmount = updated.TotalDepositAmount,
                    FirstDepositAmount = updated.FirstDepositAmount,
                    LastDepositAmount = updated.LastDepositAmount,
                    DepositCount = updated.DepositCount,
                    CurrencyCode = updated.CurrencyCode
                }, cancellationToken: ct);
            }

            return (int?)deposits.Count;
        }, ct);
    }

    // Для разового бэкфилла: собираем уникальных лидов с депозитами. Один лид может встречаться
    // в нескольких FunnelLeadState (мультибот-кампания), поэтому дедупим по (tenantId, leadId) —
    // RecalculateDeposits затем сам разошлёт события на все состояния лида.
    public async Task<IReadOnlyList<(Guid TenantId, string LeadId)>> GetLeadIdsWithDeposits(CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Gt(x => x.DepositCount, 0);

        var keys = await collection
            .Find(filter)
            .Project(x => new { x.TenantId, x.LeadId })
            .ToListAsync(ct);

        return keys
            .Select(k => (k.TenantId, k.LeadId))
            .Distinct()
            .ToList();
    }

    private sealed record DepositAggregates(decimal Total, decimal First, decimal Last, int Count, string CurrencyCode);

    // Считает депозитные агрегаты по логу событий лида. Учитываются только Accepted-события:
    // статус Duplicate зарезервирован под дедупликацию повторных постбэков по eventId.
    private async Task<DepositAggregates?> ComputeDepositAggregates(
        IClientSessionHandle session, Guid tenantId, string leadId, CancellationToken ct)
    {
        var filter = Builders<LeadEventBaseDocument>.Filter.And(
            Builders<LeadEventBaseDocument>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<LeadEventBaseDocument>.Filter.Eq(x => x.LeadId, leadId),
            Builders<LeadEventBaseDocument>.Filter.Or(
                Builders<LeadEventBaseDocument>.Filter.OfType<SaleLeadEvent>(s => s.Status == LeadEventStatus.Accepted),
                Builders<LeadEventBaseDocument>.Filter.OfType<ResaleLeadEvent>(r => r.Status == LeadEventStatus.Accepted)));

        var events = await leadEvents
            .Find(session, filter)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(ct);

        if (events.Count == 0)
            return null;

        var deposits = events
            .Select(e => e switch
            {
                SaleLeadEvent s => (Amount: s.DepositAmount, Currency: s.CurrencyCode),
                ResaleLeadEvent r => (Amount: r.DepositAmount, Currency: r.CurrencyCode),
                _ => throw new InvalidOperationException($"Unexpected lead event type: {e.GetType().Name}")
            })
            .ToList();

        return new DepositAggregates(
            Total: deposits.Sum(d => d.Amount),
            First: deposits[0].Amount,
            Last: deposits[^1].Amount,
            Count: deposits.Count,
            CurrencyCode: deposits[^1].Currency);
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

    public async Task MarkPushCompleted(Guid leadStateId, Guid pushId, CancellationToken ct)
    {
        var filter = Builders<FunnelLeadState>.Filter.Eq(x => x.Id, leadStateId);
        var update = Builders<FunnelLeadState>.Update.AddToSet(x => x.Pushes, pushId);

        await collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task Delete(Guid leadStateId, CancellationToken ct)
    {
        var result = await collection.DeleteOneAsync(x => x.Id == leadStateId, ct);

        if (result.DeletedCount == 0)
            throw new KeyNotFoundException($"Lead state '{leadStateId}' not found.");
    }

    public async Task<int> DeleteMigratedBatch(Guid tenantId, Guid botId, int limit, CancellationToken ct = default)
    {
        var filter = Builders<FunnelLeadState>.Filter.And(
            Builders<FunnelLeadState>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.BotId, botId),
            Builders<FunnelLeadState>.Filter.Eq(x => x.MigrationFrom, MigrationFrom.Chatterfy));

        var batch = await collection
            .Find(filter)
            .Project(x => new { x.Id, x.LeadId })
            .Limit(limit)
            .ToListAsync(ct);

        if (batch.Count == 0)
            return 0;

        var stateIds = batch.Select(b => b.Id).ToList();
        var leadIds = batch.Select(b => b.LeadId).Distinct().ToList();

        // Мигрированный лид входит в воронку без action/push-тасков (см. EnterFunnel: isMigrated
        // не создаёт задачи текущей ноды) — эти удаления обычно no-op, но подчищаем на случай,
        // если лид уже получил реальную активность до отката.
        await actionTasks.DeleteManyAsync(Builders<ActionTaskDocument>.Filter.In(x => x.LeadStateId, stateIds), ct);
        await pushTasks.DeleteManyAsync(Builders<PushTaskDocument>.Filter.In(x => x.LeadStateId, stateIds), ct);

        // leadId мигрированного лида либо новый (никогда не существовал), либо уже проверен на
        // коллизию при импорте в campaigns (LeadMigrationService.CreateMissing) — принадлежит тому
        // же globalId, так что здесь безопасно чистить lead_events по нему целиком, без риска
        // задеть чужого лида с тем же leadId.
        await leadEvents.DeleteManyAsync(Builders<LeadEventBaseDocument>.Filter.And(
            Builders<LeadEventBaseDocument>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<LeadEventBaseDocument>.Filter.In(x => x.LeadId, leadIds)), ct);

        var result = await collection.DeleteManyAsync(
            Builders<FunnelLeadState>.Filter.In(x => x.Id, stateIds), ct);

        return (int)result.DeletedCount;
    }
    #endregion

    #region private
    // WithTransactionAsync сам повторяет транзакцию при TransientTransactionError
    // (например, WriteConflict при конкурентной записи в тот же документ),
    // поэтому action может выполниться несколько раз до успешного коммита.
    private async Task InTransaction(Func<IClientSessionHandle, Task> action, CancellationToken ct)
    {
        using var session = await collection.Database.Client.StartSessionAsync(cancellationToken: ct);

        await session.WithTransactionAsync(async (s, _) =>
        {
            await action(s);
            return true;
        }, cancellationToken: ct);
    }

    private async Task<T> InTransaction<T>(Func<IClientSessionHandle, Task<T>> action, CancellationToken ct)
    {
        using var session = await collection.Database.Client.StartSessionAsync(cancellationToken: ct);

        return await session.WithTransactionAsync(async (s, _) => await action(s), cancellationToken: ct);
    }
    #endregion
}
