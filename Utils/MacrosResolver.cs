using states.Mongo.Documents;
using System.Text.RegularExpressions;

namespace states.Utils
{
    public static class MacrosResolver
    {
        private static readonly Regex MacroPattern = new(@"\{([\w.]+)\}", RegexOptions.Compiled);

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
                ["status"] = leadState.Status.ToString(),
                ["depValue"] = leadState.LastDepositAmount.ToString(),
                ["depSum"] = leadState.TotalDepositAmount.ToString(),
                ["depCount"] = leadState.DepositCount.ToString()
            };

            foreach (var kv in leadState.PostbackParameters)
                macros[$"pb.{kv.Key}"] = kv.Value;

            if (!string.IsNullOrEmpty(leadState.StartParameter))
            {
                var startParts = parseStartParameter(leadState.StartParameter);
                for (int i = 0; i <  startParts.Length; i++) {
                    macros.Add($"start{i}", startParts[i]);
                }
            }

            return MacroPattern.Replace(template, match =>
                macros.TryGetValue(match.Groups[1].Value, out var value) ? value ?? string.Empty : match.Value);
        }

        private static string[] parseStartParameter(string startParameter)
        {
            var splt = startParameter.Split('_');
            return splt;
        }
    }
}
