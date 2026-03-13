using System.Text.Json.Serialization;

namespace states.Services.FunnelService.Application
{    
    public enum NodeType
    {
        Start,
        ManageTag,
        SendPreset,
        ChangeFlow
    }
}
