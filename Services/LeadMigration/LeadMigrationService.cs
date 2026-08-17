using System.Collections.Concurrent;
using states.Dtos.Leads;
using states.Mongo.Repositories;
using states.Services.LeadService;

namespace states.Services.LeadMigration;

public class LeadMigrationService : ILeadMigrationService
{
    // EnterFunnel по одному лиду — несколько небольших Mongo-записей (lead_states + outbox),
    // не одна долгая транзакция на всю пачку, поэтому ограниченный параллелизм здесь безопасен —
    // тот же принцип, что и в tgengine (ChatMigrationApplicationService.ImportChats).
    private const int Concurrency = 8;

    // Удаление тоже идёт пачками, а не одним запросом на весь bot — тот же принцип, что и в
    // tgengine/campaigns (см. DeleteMigrated).
    private const int DeleteBatchSize = 2000;
    private static readonly TimeSpan DeleteBatchDelay = TimeSpan.FromMilliseconds(100);

    private readonly ILeadProgressionService leadProgressionService;
    private readonly ILeadStateRepository leadStateRepository;
    private readonly ILogger<LeadMigrationService> logger;

    public LeadMigrationService(
        ILeadProgressionService leadProgressionService,
        ILeadStateRepository leadStateRepository,
        ILogger<LeadMigrationService> logger)
    {
        this.leadProgressionService = leadProgressionService;
        this.leadStateRepository = leadStateRepository;
        this.logger = logger;
    }

    public async Task<MaterializeLeadStatesResultDto> MaterializeLeadStates(
        MaterializeLeadStatesRequestDto request,
        CancellationToken ct = default)
    {
        var results = new ConcurrentBag<MaterializeLeadStateResultDto>();

        using var gate = new SemaphoreSlim(Concurrency);

        await Task.WhenAll(request.Items.Select(async item =>
        {
            await gate.WaitAsync(ct);

            try
            {
                var enterFunnelRequest = new Dtos.Leads.EnterFunnelRequest(
                    TenantId: request.TenantId,
                    SpaceId: request.SpaceId,
                    BotId: request.BotId,
                    ChatId: item.ChatId,
                    ExternalId: item.ExternalId,
                    LeadId: item.LeadId,
                    CampaignId: item.CampaignId,
                    CampaignName: item.CampaignName,
                    SourceId: item.SourceId,
                    SourceName: item.SourceName,
                    FunnelId: item.FunnelId,
                    FlowId: item.FlowId,
                    NodeId: item.NodeId,
                    StartParameter: null,
                    MigrationFrom: Services.CampaignClient.MigrationFrom.Chatterfy,
                    Tags: item.Tags?.ToList(),
                    Status: item.Status);

                await leadProgressionService.EnterFunnel(enterFunnelRequest, ct);

                results.Add(new MaterializeLeadStateResultDto(item.ChatId, Succeeded: true, Error: null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Один упавший лид (например, funnelId не резолвится в кеше) не должен обрывать
                // всю пачку — остальные лиды миграции всё равно нужно материализовать.
                logger.LogError(ex,
                    "Lead materialization failed: tenantId={TenantId}, botId={BotId}, chatId={ChatId}, leadId={LeadId}",
                    request.TenantId, request.BotId, item.ChatId, item.LeadId);

                results.Add(new MaterializeLeadStateResultDto(item.ChatId, Succeeded: false, Error: ex.Message));
            }
            finally
            {
                gate.Release();
            }
        }));

        logger.LogInformation(
            "Lead states materialization: tenantId={TenantId}, botId={BotId}, requested={Requested}, " +
            "succeeded={Succeeded}, failed={Failed}",
            request.TenantId,
            request.BotId,
            request.Items.Count,
            results.Count(r => r.Succeeded),
            results.Count(r => !r.Succeeded));

        return new MaterializeLeadStatesResultDto(results.ToList());
    }

    public async Task<DeleteMigratedLeadStatesResultDto> DeleteMigrated(Guid tenantId, Guid botId, CancellationToken ct = default)
    {
        var totalDeleted = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var deleted = await leadStateRepository.DeleteMigratedBatch(tenantId, botId, DeleteBatchSize, ct);
            if (deleted == 0)
                break;

            totalDeleted += deleted;

            logger.LogInformation(
                "Migrated lead states rollback progress: tenantId={TenantId}, botId={BotId}, deleted={Deleted}",
                tenantId, botId, totalDeleted);

            if (deleted < DeleteBatchSize)
                break;

            await Task.Delay(DeleteBatchDelay, ct);
        }

        logger.LogInformation(
            "Migrated lead states rollback finished: tenantId={TenantId}, botId={BotId}, deleted={Deleted}",
            tenantId, botId, totalDeleted);

        return new DeleteMigratedLeadStatesResultDto(totalDeleted);
    }
}
