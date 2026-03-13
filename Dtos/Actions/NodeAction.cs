using states.Services.FunnelService.Application;
using System.Text.Json.Serialization;

namespace states.Dtos.Actions
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ManageTagAction), nameof(ActionType.ManageTag))]
    [JsonDerivedType(typeof(SendPresetAction), nameof(ActionType.SendPreset))]
    public abstract record NodeAction(
            Guid Id,            
            TimeSpan Delay
        );
}
