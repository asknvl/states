using MongoDB.Driver;
using states.Mongo.Documents;
using states.Services.FunnelService.Application;
using states.Mongo.Documents.Folders;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Documents.Outbox;
using states.Mongo.Documents.TenantTags;

namespace states.Mongo
{
    public class MongoBootstrapper
    {
        private readonly IMongoDatabase database;

        public MongoBootstrapper(IMongoDatabase database)
        {
            this.database = database;
        }

        public async Task Initialize(CancellationToken ct = default)
        {
            await CreateFunnelsCollection(ct);
            await CreateFunnelsIndexes(ct);
            await CreateLeadStatesCollection(ct);
            await CreateLeadStatesIndexes(ct);
            await CreateOutboxCollection(ct);
            await CreateOutboxIndexes(ct);
            await CreateFoldersCollection(ct);
            await CreateFoldersIndexes(ct);
            await CreateTenantTagsCollection(ct);
            await CreateTenantTagsIndexes(ct);
            await CreateActionTasksCollection(ct);
            await CreateActionTasksIndexes(ct);
            await CreatePushTasksCollection(ct);
            await CreatePushTasksIndexes(ct);
            await CreateLeadEventsCollection(ct);
            await CreateLeadEventsIndexes(ct);
        }

        #region collections
        private async Task CreateFunnelsCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("funnels"))
                return;

            await database.CreateCollectionAsync("funnels", cancellationToken: ct);
        }

        private async Task CreateLeadStatesCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("lead_states"))
                return;

            await database.CreateCollectionAsync("lead_states", cancellationToken: ct);
        }

        private async Task CreateOutboxCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("outbox"))
                return;

            await database.CreateCollectionAsync("outbox", cancellationToken: ct);
        }
        #endregion

        private async Task CreateFoldersCollection(CancellationToken ct)
        {
            var collectionNames = await database
                .ListCollectionNames()
                .ToListAsync(ct);

            if (collectionNames.Contains("folders"))
                return;

            await database.CreateCollectionAsync("folders", cancellationToken: ct);
        }

        private async Task CreateTenantTagsCollection(CancellationToken ct)
        {
            var collectionNames = await database.ListCollectionNames().ToListAsync(ct);
            if (collectionNames.Contains("tenant_tags"))
                return;
            await database.CreateCollectionAsync("tenant_tags", cancellationToken: ct);
        }

        private async Task CreateActionTasksCollection(CancellationToken ct)
        {
            var collectionNames = await database.ListCollectionNames().ToListAsync(ct);
            if (collectionNames.Contains("action_tasks"))
                return;
            await database.CreateCollectionAsync("action_tasks", cancellationToken: ct);
        }

        private async Task CreatePushTasksCollection(CancellationToken ct)
        {
            var collectionNames = await database.ListCollectionNames().ToListAsync(ct);
            if (collectionNames.Contains("push_tasks"))
                return;
            await database.CreateCollectionAsync("push_tasks", cancellationToken: ct);
        }

        private async Task CreateLeadEventsCollection(CancellationToken ct)
        {
            var collectionNames = await database.ListCollectionNames().ToListAsync(ct);
            if (collectionNames.Contains("lead_events"))
                return;
            await database.CreateCollectionAsync("lead_events", cancellationToken: ct);
        }

        #region indexes
        private async Task CreateFunnelsIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FunnelDocument>("funnels");

            var indexes = new List<CreateIndexModel<FunnelDocument>>
            {
                new CreateIndexModel<FunnelDocument>(
                    Builders<FunnelDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Descending(x => x.Id))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateLeadStatesIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FunnelLeadState>("lead_states");

            // Устаревший уникальный индекс (funnelId, leadId) от старой версии кода — без tenantId.
            // Заменён на (tenantId, funnelId, leadId) ниже. Дропаем по имени, если остался.
            try
            {
                await collection.Indexes.DropOneAsync("funnelId_1_leadId_1", ct);
            }
            catch (MongoCommandException)
            {
                // индекса уже нет — ничего страшного
            }

            var indexes = new List<CreateIndexModel<FunnelLeadState>>
            {
                // один стейт на (тенант, воронка, лид); FunnelId=null — органика, тоже уникальна по (tenantId, null, leadId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.FunnelId)
                        .Ascending(x => x.LeadId),
                    new CreateIndexOptions { Unique = true }),

                // GetLeadStateByChatId(tenantId, botId, chatId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.BotId)
                        .Ascending(x => x.ChatId)),

                // GetLeadStateByChatId(tenantId, chatId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.ChatId)),

                // GetLeadStatesByLeadId(tenantId, leadId)
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.LeadId)),

                // UpdateLeadStateStatusByChatId / MarkBlockedByChatId фильтруют только по chatId,
                // без tenantId — составной (tenantId, chatId) здесь не работает, нужен отдельный индекс
                new CreateIndexModel<FunnelLeadState>(
                    Builders<FunnelLeadState>.IndexKeys
                        .Ascending(x => x.ChatId)),
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }      

        private async Task CreateOutboxIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<OutboxDocument>("outbox");

            // Старый индекс (claimedAt, createdAt) не давал сортировку по createdAt для $or-фильтра
            // TakeNext — каждый поллинг сортировал весь бэклог в памяти. Дропаем по имени, если остался.
            try
            {
                await collection.Indexes.DropOneAsync("claimedAt_1_createdAt_1", ct);
            }
            catch (MongoCommandException)
            {
                // индекса уже нет — ничего страшного
            }

            var indexes = new List<CreateIndexModel<OutboxDocument>>
            {
                // TakeNext: скан в порядке createdAt, фильтр по claimedAt residual — первый матч
                // забирается сразу, без in-memory сортировки бэклога
                new CreateIndexModel<OutboxDocument>(
                    Builders<OutboxDocument>.IndexKeys
                        .Ascending(x => x.CreatedAt))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateFoldersIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<FolderDocument>("folders");

            var indexes = new List<CreateIndexModel<FolderDocument>>
            {
                // покрывает фильтр tenantId+funnelId и sort по order без in-memory сортировки
                new CreateIndexModel<FolderDocument>(
                    Builders<FolderDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.FunnelId)
                        .Ascending(x => x.Order))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateTenantTagsIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<TenantTag>("tenant_tags");

            var indexes = new List<CreateIndexModel<TenantTag>>
            {
                // TODO: сделать уникальным — CreateIfNeed полагается на upsert, и конкурентные вызовы
                // могут создать дубликат (tenantId, tagName). Риск низкий: операция редкая, выполняется
                // оператором вручную. Перевод в unique требует дропа/пересоздания индекса и проверки
                // базы на уже существующие дубликаты.
                new CreateIndexModel<TenantTag>(
                    Builders<TenantTag>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.TagName)),

                // UpdateTenantTagName / DecreaseTenantTagUsage фильтруют по tagId (не по _id)
                new CreateIndexModel<TenantTag>(
                    Builders<TenantTag>.IndexKeys
                        .Ascending(x => x.TagId))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateActionTasksIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<ActionTaskDocument>("action_tasks");

            // Индекс переименован (был unique_pending_ai_reply_per_lead): старый покрывал только
            // Pending и оставлял дыру, пока задача уже забрана в работу (InProgress) — за это время
            // мог создаться дублирующий AiReply. Дропаем старый по имени, если ещё остался.
            try
            {
                await collection.Indexes.DropOneAsync("unique_pending_ai_reply_per_lead", ct);
            }
            catch (MongoCommandException)
            {
                // индекса уже нет — ничего страшного
            }

            var indexes = new List<CreateIndexModel<ActionTaskDocument>>
            {
                // ClaimNext: фильтр Status=Pending + ScheduledAt<=Now, сортировка по ScheduledAt
                new CreateIndexModel<ActionTaskDocument>(
                    Builders<ActionTaskDocument>.IndexKeys
                        .Ascending(x => x.Status)
                        .Ascending(x => x.ScheduledAt)),

                // CancelPendingByLead / UnlockNext: фильтр по LeadStateId
                new CreateIndexModel<ActionTaskDocument>(
                    Builders<ActionTaskDocument>.IndexKeys
                        .Ascending(x => x.LeadStateId)
                        .Ascending(x => x.NodeId)),

                // Гарантирует не более одного активного (Pending или уже забранного в работу) AiReply на лида
                new CreateIndexModel<ActionTaskDocument>(
                    Builders<ActionTaskDocument>.IndexKeys
                        .Ascending(x => x.LeadStateId)
                        .Ascending(x => x.Type),
                    new CreateIndexOptions<ActionTaskDocument>
                    {
                        Unique = true,
                        PartialFilterExpression = Builders<ActionTaskDocument>.Filter.Or(
                            Builders<ActionTaskDocument>.Filter.And(
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.AiReply),
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending)),
                            Builders<ActionTaskDocument>.Filter.And(
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.AiReply),
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.InProgress))
                        ),
                        Name = "unique_active_ai_reply_per_lead"
                    }),

                // Не более одного активного SendTyping на лида: TryInsertSendTypingTask вставляет
                // вслепую и глотает дубликат — O(1) без скана тасков лида, гонка конкурентных
                // входящих закрыта на уровне БД (filter-upsert без unique её не закрывал бы)
                new CreateIndexModel<ActionTaskDocument>(
                    Builders<ActionTaskDocument>.IndexKeys
                        .Ascending(x => x.LeadStateId)
                        .Ascending(x => x.Type),
                    new CreateIndexOptions<ActionTaskDocument>
                    {
                        Unique = true,
                        PartialFilterExpression = Builders<ActionTaskDocument>.Filter.Or(
                            Builders<ActionTaskDocument>.Filter.And(
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.SendTyping),
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.Pending)),
                            Builders<ActionTaskDocument>.Filter.And(
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Type, ActionType.SendTyping),
                                Builders<ActionTaskDocument>.Filter.Eq(x => x.Status, ActionStatus.InProgress))
                        ),
                        Name = "unique_active_send_typing_per_lead"
                    }),

                // TTL: терминальные таски (Completed/Failed/Cancelled) получают finishedAt и удаляются
                // Mongo автоматически спустя retention — коллекция не растёт бесконечно. Активные таски
                // поля не имеют и под TTL не попадают. При изменении срока Mongo кинет IndexOptionsConflict —
                // тогда сначала дропнуть индекс по имени (или collMod), как сделано выше для переименованного.
                new CreateIndexModel<ActionTaskDocument>(
                    Builders<ActionTaskDocument>.IndexKeys.Ascending(x => x.FinishedAt),
                    new CreateIndexOptions
                    {
                        ExpireAfter = TimeSpan.FromDays(7),
                        Name = "ttl_finished_action_tasks"
                    })
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);

            // Бэкфилл: терминальные таски, созданные до появления finishedAt, без него не удалятся
            // никогда. Идемпотентно — повторный старт ничего не перезапишет.
            var backfillFilter = Builders<ActionTaskDocument>.Filter.And(
                Builders<ActionTaskDocument>.Filter.In(x => x.Status,
                    new[] { ActionStatus.Completed, ActionStatus.Failed, ActionStatus.Cancelled }),
                Builders<ActionTaskDocument>.Filter.Eq(x => x.FinishedAt, null));

            await collection.UpdateManyAsync(
                backfillFilter,
                Builders<ActionTaskDocument>.Update.Set(x => x.FinishedAt, DateTime.UtcNow),
                cancellationToken: ct);
        }

        private async Task CreatePushTasksIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<PushTaskDocument>("push_tasks");

            var indexes = new List<CreateIndexModel<PushTaskDocument>>
            {
                // ClaimNext: фильтр Status=Pending + ScheduledAt<=Now, сортировка по ScheduledAt
                new CreateIndexModel<PushTaskDocument>(
                    Builders<PushTaskDocument>.IndexKeys
                        .Ascending(x => x.Status)
                        .Ascending(x => x.ScheduledAt)),

                // CancelPendingByLead / UnlockNext: фильтр по LeadStateId
                new CreateIndexModel<PushTaskDocument>(
                    Builders<PushTaskDocument>.IndexKeys
                        .Ascending(x => x.LeadStateId)
                        .Ascending(x => x.NodeId))
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        private async Task CreateLeadEventsIndexes(CancellationToken ct)
        {
            var collection = database.GetCollection<LeadEventBaseDocument>("lead_events");

            // Первая версия дедупликационного индекса — unique+SPARSE (tenantId, eventId) — в расчёте,
            // что события без eventId в индекс не попадут. Для составного sparse-индекса это не так:
            // документ пропускается, только если у него нет НИ ОДНОГО поля ключа, а tenantId есть у всех.
            // События без eventId (BotActivation/Contact/...) индексировались как eventId:null, unique
            // разрешал одно такое на тенанта — второе падало E11000, обработчик Kafka обрывался до
            // EnterFunnel/MarkLeadBlocked, сообщение коммитилось и терялось (инцидент 2026-08-18).
            // Дропаем по имени, если остался; замена — partial-индекс ниже.
            try
            {
                await collection.Indexes.DropOneAsync("tenantId_1_eventId_1", ct);
            }
            catch (MongoCommandException)
            {
                // индекса уже нет — ничего страшного
            }

            var indexes = new List<CreateIndexModel<LeadEventBaseDocument>>
            {
                // GetByLead(tenantId, spaceId, leadId): покрывает фильтр и сортировку по createdAt в одном проходе
                new CreateIndexModel<LeadEventBaseDocument>(
                    Builders<LeadEventBaseDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.SpaceId)
                        .Ascending(x => x.LeadId)
                        .Ascending(x => x.CreatedAt)),

                // ComputeDepositAggregates (пересчёт депозитных агрегатов в lead_states): фильтр по
                // tenantId+leadId без spaceId и сортировка по createdAt. Residual-фильтр по типу
                // события (_t) и статусу дешёвый — событий на одного лида немного.
                new CreateIndexModel<LeadEventBaseDocument>(
                    Builders<LeadEventBaseDocument>.IndexKeys
                        .Ascending(x => x.TenantId)
                        .Ascending(x => x.LeadId)
                        .Ascending(x => x.CreatedAt)),

                // Дедупликация по внешнему eventId: повторный/резюмированный прогон импорта миграции
                // (LeadEventsApplicationService.ImportEvents) и повторная доставка постбэков. Unique +
                // PARTIAL ($exists: true): под индекс и ограничение попадают только документы, у которых
                // поле eventId есть (Registration/Sale/Resale — см. типы-наследники); события бота без
                // eventId не индексируются вовсе, в отличие от sparse (см. комментарий у дропа выше).
                // Имя задано явно: автогенерённое совпало бы с tenantId_1_eventId_1, и drop-by-name выше
                // сносил бы этот индекс на каждом старте.
                new CreateIndexModel<LeadEventBaseDocument>(
                    Builders<LeadEventBaseDocument>.IndexKeys
                        .Ascending("tenantId")
                        .Ascending("eventId"),
                    new CreateIndexOptions<LeadEventBaseDocument>
                    {
                        Unique = true,
                        PartialFilterExpression = Builders<LeadEventBaseDocument>.Filter.Exists("eventId"),
                        Name = "unique_external_eventId_per_tenant"
                    })
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
        }

        #endregion
    }
}
