using MongoDB.Bson;
using MongoDB.Driver;
using states.Mongo;
using states.Mongo.Documents;
using states.Services.Events.Producer;
using states.Services.Events.Producer.Payloads;
using states.Services.FunnelService.Runtime;

namespace states.Services.LeadService.Worker;

public sealed class LeadStateChangeStreamWorker(
    MongoContext context,
    IEventService eventService,
    IFunnelRuntimeCache funnelCache,
    ILogger<LeadStateChangeStreamWorker> logger) : BackgroundService
{
    private const string CheckpointId = "lead_states_change_stream";

    // insert — любое создание документа
    // update — только если изменился status или nodeId
    private static readonly PipelineDefinition<ChangeStreamDocument<FunnelLeadState>, ChangeStreamDocument<FunnelLeadState>> Pipeline =
        new BsonDocument[]
        {
            new("$match", new BsonDocument
            {
                { "$or", new BsonArray
                    {
                        new BsonDocument("operationType", "insert"),
                        new BsonDocument("$and", new BsonArray
                        {
                            new BsonDocument("operationType", "update"),
                            new BsonDocument("$or", new BsonArray
                            {
                                new BsonDocument("updateDescription.updatedFields.status", new BsonDocument("$exists", true)),
                                new BsonDocument("updateDescription.updatedFields.nodeId",  new BsonDocument("$exists", true))
                            })
                        })
                    }
                }
            })
        };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // не блокируем хост при старте

        logger.LogInformation("LeadStateChangeStreamWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var resumeToken = await LoadResumeToken(stoppingToken);

                var options = new ChangeStreamOptions
                {
                    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                    ResumeAfter = resumeToken
                };

                using var cursor = await context.LeadStates.WatchAsync(Pipeline, options, stoppingToken);

                while (await cursor.MoveNextAsync(stoppingToken))
                {
                    foreach (var change in cursor.Current)
                    {
                        // Сначала публикуем — если Kafka недоступна, бросит исключение.
                        // Resume token не сохраняется, при рестарте событие повторится.
                        await HandleChange(change, stoppingToken);

                        // Сохраняем только после успешной публикации
                        await SaveResumeToken(change.ResumeToken, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Change stream error, restarting in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("LeadStateChangeStreamWorker stopped");
    }

    private async Task HandleChange(ChangeStreamDocument<FunnelLeadState> change, CancellationToken ct)
    {
        var doc = change.FullDocument;
        if (doc is null)
        {
            logger.LogWarning("FullDocument is null for change stream event (documentKey={DocumentKey}), skipping",
                change.DocumentKey);
            return;
        }

        if (change.OperationType == ChangeStreamOperationType.Insert)
        {
            await PublishCreated(doc, ct);
            return;
        }

        var updatedFields = change.UpdateDescription?.UpdatedFields;
        if (updatedFields is null) return;

        if (updatedFields.Contains("status"))
            await PublishStatusChanged(doc, ct);

        if (updatedFields.Contains("nodeId"))
            await PublishNodeChanged(doc, ct);
    }

    private Task PublishCreated(FunnelLeadState doc, CancellationToken ct)
    {
        var payload = new LeadStateCreatedPayload(
            doc.Id,
            doc.TenantId,
            doc.SpaceId,
            doc.BotId,
            doc.ChatId,
            doc.CampaignId,
            doc.LeadId,
            doc.FunnelId,
            doc.FlowId,
            doc.NodeId,
            doc.Status,
            doc.Version);

        return eventService.Publish(new LeadStateCreatedEvent(payload), ct);
    }

    private Task PublishStatusChanged(FunnelLeadState doc, CancellationToken ct)
    {
        var payload = new LeadStatusChangedPayload(
            doc.Id,
            doc.TenantId,
            doc.ChatId,
            doc.Status,
            doc.Version);

        return eventService.Publish(new LeadStatusChangedEvent(payload), ct);
    }

    private Task PublishNodeChanged(FunnelLeadState doc, CancellationToken ct)
    {
        var nodeLabel = ResolveNodeLabel(doc.FunnelId, doc.FlowId, doc.NodeId);

        var payload = new LeadNodeChangedPayload(
            doc.Id,
            doc.TenantId,
            doc.ChatId,
            doc.NodeId,
            nodeLabel,
            doc.Version);

        return eventService.Publish(new LeadNodeChangedEvent(payload), ct);
    }

    private string? ResolveNodeLabel(Guid funnelId, Guid flowId, Guid nodeId)
    {
        var funnel = funnelCache.GetFunnel(funnelId);
        if (funnel is null) return null;

        var flow = funnel.Flows.FirstOrDefault(f => f.Id == flowId);
        if (flow is null) return null;

        return flow.Nodes.FirstOrDefault(n => n.Id == nodeId)?.Data.Label;
    }

    // --- Resume token ---

    private async Task<BsonDocument?> LoadResumeToken(CancellationToken ct)
    {
        var checkpoint = await context.ChangeStreamCheckpoints
            .Find(x => x.Id == CheckpointId)
            .FirstOrDefaultAsync(ct);

        if (checkpoint is null)
        {
            logger.LogInformation("No change stream checkpoint found, starting from now");
            return null;
        }

        logger.LogInformation("Resuming change stream from saved checkpoint");
        return checkpoint.ResumeToken;
    }

    private Task<ReplaceOneResult> SaveResumeToken(BsonDocument resumeToken, CancellationToken ct)
    {
        var checkpoint = new ChangeStreamCheckpoint
        {
            Id = CheckpointId,
            ResumeToken = resumeToken
        };

        return context.ChangeStreamCheckpoints.ReplaceOneAsync(
            x => x.Id == CheckpointId,
            checkpoint,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }
}
