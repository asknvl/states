using states.Mongo.Documents.Outbox;
using states.Mongo.Repositories;
using states.Services.Events.Producer;
using states.Services.Events.Producer.Payloads;
using states.Services.FunnelService.Runtime;
using FunnelTag = states.Dtos.Funnels.Tag;

namespace states.Services.LeadService.Worker;

public sealed class OutboxWorkerService(
    IOutboxRepository outboxRepository,
    IEventService eventService,
    IFunnelRuntimeCache funnelCache,
    ILogger<OutboxWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        logger.LogInformation("OutboxWorkerService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var doc = await outboxRepository.TakeNext(ClaimTimeout, stoppingToken);

                if (doc is null)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                await Process(doc, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing error, retrying after delay");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("OutboxWorkerService stopped");
    }

    private async Task Process(OutboxDocument doc, CancellationToken ct)
    {
        try
        {
            switch (doc)
            {
                case LeadStateCreatedOutboxDocument created:
                    await eventService.Publish(new LeadStateCreatedEvent(new LeadStateCreatedPayload(
                        created.TenantId, created.SpaceId, created.BotId, created.ChatId,
                        created.LeadId, created.CampaignId, created.FunnelId,
                        created.FlowId, created.NodeId, created.Status, created.Version)), ct);
                    break;

                case LeadStatusChangedOutboxDocument status:
                    await eventService.Publish(new LeadStatusChangedEvent(new LeadStatusChangedPayload(
                        status.TenantId, status.SpaceId, status.BotId, status.ChatId,
                        status.LeadId, status.Status, status.Version)), ct);
                    break;

                case LeadNodeChangedOutboxDocument node:
                    var nodeLabel = ResolveNodeLabel(node.FunnelId, node.FlowId, node.NodeId);
                    await eventService.Publish(new LeadNodeChangedEvent(new LeadNodeChangedPayload(
                        node.TenantId, node.SpaceId, node.BotId, node.ChatId,
                        node.LeadId, node.NodeId, nodeLabel, node.Version)), ct);
                    break;

                case LeadTagChangedOutboxDocument tags:
                    var resolvedTags = ResolveTags(tags.FunnelId, tags.Tags);
                    await eventService.Publish(new LeadTagChangedEvent(new LeadTagChangedPayload(
                        tags.TenantId, tags.SpaceId, tags.BotId, tags.ChatId,
                        tags.LeadId, resolvedTags, tags.Version)), ct);
                    break;
            }

            await outboxRepository.Delete(doc.Id, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process outbox document {Id}, will retry on next claim", doc.Id);
        }
    }

    private string? ResolveNodeLabel(Guid funnelId, Guid flowId, Guid nodeId)
    {
        var funnel = funnelCache.GetFunnel(funnelId);
        if (funnel is null) return null;
        var flow = funnel.Flows.FirstOrDefault(f => f.Id == flowId);
        return flow?.Nodes.FirstOrDefault(n => n.Id == nodeId)?.Data.Label;
    }

    private List<FunnelTag> ResolveTags(Guid funnelId, List<Guid> tagIds)
    {
        var funnel = funnelCache.GetFunnel(funnelId);
        if (funnel is null) return [];
        return funnel.Tags.Where(t => tagIds.Contains(t.Id)).ToList();
    }
}
