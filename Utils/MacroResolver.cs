using states.Mongo.Documents;
using System.Text.RegularExpressions;

namespace states.Utils
{
    public static class MacroResolver
    {
        private static readonly Regex MacroPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

        public static string Resolve(string template, FunnelLeadState leadState)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            var macros = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenantId"] = leadState.TenantId.ToString(),
                ["spaceId"] = leadState.SpaceId.ToString(),
                ["botId"] = leadState.BotId.ToString(),
                ["chatId"] = leadState.ChatId.ToString(),
                ["leadId"] = leadState.LeadId,
                ["campaignId"] = leadState.CampaignId?.ToString(),
                ["campaignName"] = leadState.CampaignName,
                ["sourceId"] = leadState.SourceId,
                ["sourceName"] = leadState.SourceName,
                ["funnelId"] = leadState.FunnelId?.ToString(),
                ["funnelName"] = leadState.FunnelName,
                ["flowId"] = leadState.FlowId?.ToString(),
                ["flowName"] = leadState.FlowName,
                ["nodeId"] = leadState.NodeId?.ToString(),
                ["nodeLabel"] = leadState.NodeLabel,
                ["status"] = leadState.Status.ToString()
            };

            return MacroPattern.Replace(template, match =>
                macros.TryGetValue(match.Groups[1].Value, out var value) ? value ?? string.Empty : match.Value);
        }
    }
}
