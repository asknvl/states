using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace states.Services.MigratorService;

public class MigratorClient(HttpClient http, ILogger<MigratorClient> logger) : IMigratorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // migrator отдаёт статус строкой с теми же именами, что у LeadFunnelStatus.
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<MigratedLeadState?> GetMigratedLeadState(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        CancellationToken ct)
    {
        var url = $"/migrated-leads/state?tenantId={tenantId}&botId={botId}&globalId={globalId}";

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

        // 404 — лид не выгружался migrator'ом, применять нечего.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        return await JsonSerializer.DeserializeAsync<MigratedLeadState>(stream, JsonOptions, ct);
    }
}
