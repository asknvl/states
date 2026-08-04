using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using states.Services.Events.Consumers.LeadPostbackEvents;

namespace states.Services.CampaignService;

public class CampaignClient(HttpClient http, ILogger<CampaignClient> logger) : ICampaignClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // campaigns сериализует enum'ы строками (MigrationFrom: "None"/"Chatterfy") —
        // без конвертера десериализация FunnelEntryPoint падала бы на этом поле.
        Converters = { new JsonStringEnumConverter() }
    };

    //
    public async Task<FunnelEntryPoint> GetFunnelEntryPoint(
        Guid tenantId,
        Guid botId,
        Guid globalId,
        string? startParameter,
        CancellationToken ct)
    {
        var url = $"/leads/entry?tenantId={tenantId}&botId={botId}&globalId={globalId}";

        if (!string.IsNullOrEmpty(startParameter))
            url += $"&startParameter={Uri.EscapeDataString(startParameter)}";

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CampaignClient request failed: {Url}", url);
            throw;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        return await JsonSerializer.DeserializeAsync<FunnelEntryPoint>(stream, JsonOptions, ct); //TODO разобраться тут чтобы всегда что-то возврашало
    }

    public async Task<EntryPointDto?> GetAutoActionEntryPoint(
        Guid tenantId,
        Guid campaignId,
        PostbackEventType postbackEventType,
        int depositCount,
        CancellationToken ct)
    {
        var url = $"/leads/autoaction?tenantId={tenantId}&campaignId={campaignId}&postbackEvent={postbackEventType}&depositCount={depositCount}";

        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CampaignClient request failed: {Url}", url);
            throw;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        return await JsonSerializer.DeserializeAsync<EntryPointDto>(stream, JsonOptions, ct);
    }
}
