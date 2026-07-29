namespace states.Services.MigratorService;

public interface IMigratorClient
{
    /// <summary>
    /// Статус и теги лида, выгруженного из внешнего сервиса, — применяются при входе
    /// в воронку. Null — лид не выгружался.
    /// </summary>
    Task<MigratedLeadState?> GetMigratedLeadState(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        CancellationToken ct);
}
