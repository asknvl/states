namespace states.Services.MigratorService;

public interface IMigratorClient
{
    /// <summary>
    /// Теги выгруженного из внешнего сервиса лида, уже переведённые migrator'ом
    /// в наши идентификаторы по соответствиям «было — стало».
    /// Null — лид не выгружался.
    /// </summary>
    Task<IReadOnlyList<MigratedLeadTag>?> GetMigratedLeadTags(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        CancellationToken ct);
}
