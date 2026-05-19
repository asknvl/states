using System.Text.Json.Serialization;

namespace states.Services.FunnelService.Application
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RecognitionType
    {
        Enabled,
        Manual,
        Skip
    }
}
