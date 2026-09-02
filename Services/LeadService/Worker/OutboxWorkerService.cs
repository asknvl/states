using states.Mongo.Documents.Outbox;
using states.Mongo.Mappers;
using states.Mongo.Repositories;
using states.Services.Events.Producer;
using states.Services.Events.Producer.Payloads;
using states.Services.Events.Producer.Payloads.Conversions;
using states.Services.Events.Producer.Payloads.LeadState;
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

        LeadStateChangeEventPayloadBase payload = null;

        try
        {
            switch (doc)
            {
                case LeadStateCreatedOutboxDocument created:

                    var scp = new LeadStateCreatedPayload(
                            TenantId: created.TenantId,
                            SpaceId: created.SpaceId,
                            BotId: created.BotId,
                            ChatId: created.ChatId,
                            LeadId: created.LeadId,
                            CampaignId: created.CampaignId,
                            CampaignName: created.CampaignName,
                            SourceId: created.SourceId,
                            SourceName: created.SourceName,
                            FunnelId: created.FunnelId,
                            FunnelName: created.FunnelName,
                            FlowId: created.FlowId,
                            FlowName: created.FlowName,
                            NodeId: created.NodeId,
                            NodeLabel: created.NodeLabel,
                            Status: created.Status,
                            IsInputTranslatorOn: created.IsInputTranslatorOn,
                            IsOutputTranslatorOn: created.IsOutputTranslatorOn,
                            PhotoRecognition: created.PhotoRecognition,
                            VideoRecognition: created.VideoRecognition,
                            VoiceRecognition: created.VoiceRecognition,
                            Version: created.Version,
                            PostbackParameters: created.PostbackParameters is { Count: > 0 }
                                ? created.PostbackParameters
                                : null,
                            Tags: created.Tags is { Count: > 0 }
                                ? created.Tags.Select(t => t.ToDto()).ToList()
                                : null);

                    await eventService.Publish(new LeadStateCreatedEvent(scp), ct);

                    break;

                case LeadStatusChangedOutboxDocument status:

                    var lsp = new LeadStatusChangedPayload(
                        status.TenantId,
                        status.SpaceId,
                        status.BotId,
                        status.ChatId,
                        status.LeadId,
                        status.Status,
                        status.Version);

                    await eventService.Publish(new LeadStatusChangedEvent(lsp), ct);

                    break;

                case LeadFunnelPositionChangedOutboxDocument position:

                    var nodeLabel = ResolveNodeLabel(position.FunnelId, position.FlowId, position.NodeId);

                    var lnp = new LeadFunnelPositionChangedPayload(
                            TenantId: position.TenantId,
                            SpaceId: position.SpaceId,
                            BotId: position.BotId,
                            ChatId: position.ChatId,
                            LeadId: position.LeadId,
                            FunnelId: position.FunnelId,
                            FunnelName: position.FunnelName,
                            FlowId: position.FlowId,
                            FlowName: position.FlowName,
                            NodeId: position.NodeId,
                            NodeLabel: position.NodeLabel,
                            Status: position.Status,
                            Version: position.Version
                        );

                    await eventService.Publish(new LeadFunnelPositionChangedEvent(lnp), ct);
                    break;

                case LeadTagChangedOutboxDocument tags:

                    var ltc = new LeadTagsChangedPayload(
                        TenantId: tags.TenantId,
                        SpaceId: tags.SpaceId,
                        BotId: tags.BotId,
                        ChatId: tags.ChatId,
                        LeadId: tags.LeadId,
                        Tags: tags.Tags.Select(t => t.ToDto()).ToList(),
                        Operation: tags.Operation,
                        Tag: tags.Tag.ToDto(),
                        Version: tags.Version);

                    await eventService.Publish(new LeadTagChangedEvent(ltc), ct);
                    break;

                case LeadTranslatorChangedOutboxDocument translator:

                    var ltr = new LeadTranslatorChangedPayload(
                        TenantId: translator.TenantId,
                        SpaceId: translator.SpaceId,
                        BotId: translator.BotId,
                        ChatId: translator.ChatId,
                        LeadId: translator.LeadId,
                        IsInputTranslatorOn: translator.IsInputTranslatorOn,
                        IsOutputTranslatorOn: translator.IsOutputTranslatorOn,
                        Version: translator.Version);

                    await eventService.Publish(new LeadTranslatorChangedEvent(ltr), ct);
                    break;

                case LeadPostbackParametersChangedOutboxDocument postbackParameters:

                    var lpp = new LeadPostbackParametersChangedPayload(
                        TenantId: postbackParameters.TenantId,
                        SpaceId: postbackParameters.SpaceId,
                        BotId: postbackParameters.BotId,
                        ChatId: postbackParameters.ChatId,
                        LeadId: postbackParameters.LeadId,
                        PostbackParameters: postbackParameters.PostbackParameters,
                        Version: postbackParameters.Version);

                    await eventService.Publish(new LeadPostbackParametersChangedEvent(lpp), ct);
                    break;

                case LeadDepositChangedOutboxDocument deposit:

                    var ldc = new LeadDepositChangedPayload(
                        TenantId: deposit.TenantId,
                        SpaceId: deposit.SpaceId,
                        BotId: deposit.BotId,
                        ChatId: deposit.ChatId,
                        LeadId: deposit.LeadId,
                        TotalDepositAmount: deposit.TotalDepositAmount,
                        FirstDepositAmount: deposit.FirstDepositAmount,
                        LastDepositAmount: deposit.LastDepositAmount,
                        DepositCount: deposit.DepositCount,
                        CurrencyCode: deposit.CurrencyCode,
                        Version: deposit.Version);

                    await eventService.Publish(new LeadDepositChangedEvent(ldc), ct);
                    break;

                case LeadConversionOutboxDocument conversion:

                    var lcp = new LeadConversionPayload(
                        ConversionEventId: conversion.Id,
                        Type: conversion.ConversionType,
                        TenantId: conversion.TenantId,
                        SpaceId: conversion.SpaceId,
                        CampaignId: conversion.CampaignId,
                        LeadId: conversion.LeadId,
                        OccurredAt: conversion.OccurredAt,
                        Amount: conversion.Amount,
                        Currency: conversion.Currency);

                    await eventService.Publish(new LeadConversionEvent(lcp), ct);
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
