using states.Dtos.LeadEvents;
using states.Mongo.Documents.LeadEvents;
using states.Mongo.Repositories;
using states.Services.LeadEventsService.Mapping;

namespace states.Services.LeadEventsService.Application
{
    public class LeadEventsApplicationService(
        ILeadEventsRepository leadEventsRepository,
        ILeadStateRepository leadStateRepository,
        ILogger<LeadEventsApplicationService> logger) : ILeadEventsApplicationService
    {
        public async Task<IReadOnlyCollection<LeadEventBaseDto>> GetLeadEvents(
            Guid tenantId,
            Guid spaceId,
            string leadId,
            IReadOnlyCollection<LeadEventTypes>? eventTypes,
            CancellationToken ct = default)
        {
            var documents = await leadEventsRepository.GetByLead(tenantId, spaceId, leadId, eventTypes, ct);
            return documents.Select(d => d.ToDto()).ToList();
        }

        public IReadOnlyCollection<LeadEventTypes> GetLeadEventTypes()
        {
            return Enum.GetValues<LeadEventTypes>();
        }

        public async Task<ImportLeadEventsResultDto> ImportEvents(
            ImportLeadEventsRequestDto request,
            CancellationToken ct = default)
        {
            if (request.Items.Count == 0)
                return new ImportLeadEventsResultDto(Imported: 0, NotImported: 0, Skipped: 0);

            var documents = new List<LeadEventBaseDocument>(request.Items.Count);
            var skipped = 0;

            foreach (var item in request.Items)
            {
                var document = BuildDocument(request.TenantId, request.SpaceId, item);

                if (document is null)
                {
                    skipped++;
                    continue;
                }

                documents.Add(document);
            }

            var inserted = await leadEventsRepository.CreateMany(documents, ct);

            // ВАЖНО: RecalculateDeposits обновляет и шлёт LeadDepositChangedEvent в tgengine только
            // для УЖЕ существующих FunnelLeadState — если материализация лида (см. LeadMigrationService.
            // MaterializeLeadStates) ещё не проходила, ниже будет no-op и тишина, БЕЗ ошибки. При этом
            // CreateLeadState сам считает депозиты при создании состояния (ComputeDepositAggregates),
            // но это ничего не даёт для tgengine: LeadStateCreatedEvent депозитных полей не несёт,
            // Postgres-таблицу lead_deposits обновляет исключительно LeadDepositChangedEvent. Поэтому
            // миграция обязана вызывать сначала /lead-migration/materialize, потом /lead-events/import —
            // в обратном порядке депозиты останутся верными в Mongo, но не долетят до tgengine.
            var depositLeadIds = inserted
                .Where(d => d is SaleLeadEvent or ResaleLeadEvent)
                .Select(d => d.LeadId)
                .Distinct()
                .ToList();

            foreach (var leadId in depositLeadIds)
                await leadStateRepository.RecalculateDeposits(request.TenantId, leadId, ct);

            logger.LogInformation(
                "Lead events import: tenantId={TenantId}, requested={Requested}, imported={Imported}, " +
                "notImported={NotImported}, skipped={Skipped}",
                request.TenantId,
                request.Items.Count,
                inserted.Count,
                documents.Count - inserted.Count,
                skipped);

            return new ImportLeadEventsResultDto(
                Imported: inserted.Count,
                NotImported: documents.Count - inserted.Count,
                Skipped: skipped);
        }

        // Registration/Sale/Resale — единственные типы, для которых у states есть достаточно данных
        // (сумма, валюта) и документный тип. Остальные значения LeadEventTypes либо не несут смысла
        // как исторический факт трекера (BotActivation/BotDeactivation/Contact/ChannelSubscribtion —
        // это события нашего бота, не внешнего сервиса), либо для них ещё нет документного типа
        // в states (Comission/Withdraw*) — такие элементы пропускаются (см. Skipped).
        private static LeadEventBaseDocument? BuildDocument(Guid tenantId, Guid spaceId, ImportLeadEventItemDto item)
        {
            return item.Type switch
            {
                LeadEventTypes.Registration => new RegistrationLeadEvent
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    SpaceId = spaceId,
                    LeadId = item.LeadId,
                    EventId = item.ExternalEventId,
                    Status = LeadEventStatus.Accepted,
                    CreatedAt = item.OccurredAt,
                    CurrencyCode = item.CurrencyCode ?? string.Empty
                },

                LeadEventTypes.Sale => new SaleLeadEvent
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    SpaceId = spaceId,
                    LeadId = item.LeadId,
                    EventId = item.ExternalEventId,
                    Status = LeadEventStatus.Accepted,
                    CreatedAt = item.OccurredAt,
                    DepositAmount = item.Amount,
                    CurrencyCode = item.CurrencyCode ?? string.Empty
                },

                LeadEventTypes.Resale => new ResaleLeadEvent
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    SpaceId = spaceId,
                    LeadId = item.LeadId,
                    EventId = item.ExternalEventId,
                    Status = LeadEventStatus.Accepted,
                    CreatedAt = item.OccurredAt,
                    DepositAmount = item.Amount,
                    CurrencyCode = item.CurrencyCode ?? string.Empty
                },

                _ => null
            };
        }
    }
}
