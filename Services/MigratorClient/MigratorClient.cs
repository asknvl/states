using System.Net;
using System.Text.Json;

namespace states.Services.MigratorService;

public class MigratorClient(HttpClient http, ILogger<MigratorClient> logger) : IMigratorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<MigratedLeadTag>?> GetMigratedLeadTags(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        CancellationToken ct)
    {
        var url = $"/migrated-leads/tags?tenantId={tenantId}&botId={botId}&globalId={globalId}";

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MigratorClient request failed: {Url}", url);
            throw;
        }

        // 404 — лид не выгружался migrator'ом, тегов для переноса нет.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        return await JsonSerializer.DeserializeAsync<List<MigratedLeadTag>>(stream, JsonOptions, ct);
    }
}
