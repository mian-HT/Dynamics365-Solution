namespace XgateSmsChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using System;
    using System.Collections.Generic;

    // This is a plugin for sending delivery report to Channel Definition Delivery report API
    // which should be called by a proxy service that processes incoming notifications from service provider
    public class DeliveryReportPlugin : IPlugin
    {
        // 定义在 /xgatesmschannel.solution/unmanaged/Other/relationships/customizations.xml 中
        private static readonly Guid ChannelDefinitionId = Guid.Parse("d78c79f8-d725-41ff-b0de-87988baf2643");

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing delivery report plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();
            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);
            var organizationService = serviceProvider.Get<IOrganizationServiceFactory>().CreateOrganizationService(null);

            var queryParams = ParseQueryString(payload);
            queryParams.TryGetValue("externalId", out var rawExternalId);
            queryParams.TryGetValue("state", out var state);
            queryParams.TryGetValue("messageID", out var messageID);

            var fromNumber = string.Empty;
            var d365MessageId = ResolveD365MessageId(rawExternalId, tracingService, ref fromNumber);

            var deliveryReport = new DeliveryReport()
            {
                ChannelDefinitionId = ChannelDefinitionId,
                MessageId = messageID,
                RequestId = d365MessageId,
                Status = MapState(state),
                From = fromNumber,
                OrganizationId = pluginExecutionContext.OrganizationId.ToString(),
                StatusDetails = new Dictionary<string, object>()
            };

            var notificatonPayload = JsonUtils.Serialize(deliveryReport);
            tracingService.Trace("Notification payload: {0}", notificatonPayload);

            var response = organizationService.Execute(new OrganizationRequest("msdyn_D365ChannelsNotification")
            {
                Parameters =
                {
                    { "notificationPayLoad", notificatonPayload }
                }
            });

            pluginExecutionContext.OutputParameters["response"] = response.Results["responseMessage"];
        }

        // 把网关回执的 query string (a=b&c=d) 解析为大小写不敏感的字典
        private static Dictionary<string, string> ParseQueryString(string payload)
        {
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(payload))
            {
                return queryParams;
            }

            foreach (var pair in payload.Split('&'))
            {
                var idx = pair.IndexOf('=');
                if (idx < 0)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(pair.Substring(0, idx)).Trim();
                var value = Uri.UnescapeDataString(pair.Substring(idx + 1)).Trim();
                queryParams[key] = value;
            }

            return queryParams;
        }

        // 从 "{msgB64}.{from}" 形式的 externalId 中还原 D365 流水号与发件人号码
        private static string ResolveD365MessageId(string rawExternalId, ITracingService tracingService, ref string fromNumber)
        {
            var d365MessageId = rawExternalId;
            if (string.IsNullOrEmpty(rawExternalId) || !rawExternalId.Contains("."))
            {
                return d365MessageId;
            }

            var parts = rawExternalId.Split('.');
            try
            {
                var msgB64 = parts[0].Replace("-", "+").Replace("_", "/");
                var padding = msgB64.Length % 4;
                if (padding > 0)
                {
                    msgB64 += new string('=', 4 - padding);
                }

                d365MessageId = new Guid(Convert.FromBase64String(msgB64)).ToString();
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                // externalId 前缀不是合法 Base64/Guid 时保留原始值，仅记录日志便于排查
                tracingService.Trace("ExternalId 解码失败，保留原始值: " + ex.Message);
            }

            if (parts.Length >= 2)
            {
                fromNumber = Uri.UnescapeDataString(parts[1]);
            }

            return d365MessageId;
        }

        private static string MapState(string state)
        {
            if (string.Equals(state, "DELIVRD", StringComparison.OrdinalIgnoreCase))
            {
                return "Delivered";
            }

            if (string.Equals(state, "UNDELIV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "REJECTED", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed";
            }

            return "Sent";
        }
    }
}
