namespace XgateSmsChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using XgateSmsChannel.Plugins;
    using System;
    using System.Collections.Generic;

    // This is a plugin for sending delivery report to Channel Definition Delivery report API
    // which should be called by a proxy service that processes incoming notifications from serice provider
    public class DeliveryReportPlugin : IPlugin
    {
        private static string DecodeBase64UrlGuid(string base64Url)
        {
            var base64 = base64Url.Replace("-", "+").Replace("_", "/") + "==";
            var bytes = Convert.FromBase64String(base64);
            return new Guid(bytes).ToString();
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing delivery report plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();
            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);
            var organizationService = serviceProvider.Get<IOrganizationServiceFactory>().CreateOrganizationService(null);

            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(payload))
            {
                var pairs = payload.Split('&');
                foreach (var pair in pairs)
                {
                    var idx = pair.IndexOf('=');
                    if (idx < 0) continue;
                    var key = Uri.UnescapeDataString(pair.Substring(0, idx)).Trim();
                    var value = Uri.UnescapeDataString(pair.Substring(idx + 1)).Trim();
                    queryParams[key] = value;
                }
            }

            // 1. 从回执中拿到你们原样弹回来的拼接字符串
            queryParams.TryGetValue("externalId", out var rawExternalId);
            queryParams.TryGetValue("state", out var state);
            queryParams.TryGetValue("messageID", out var messageID);

            // 2. 拆解我们精简后的包裹
            string d365MessageId = rawExternalId;
            string fromNumber = "";

            if (!string.IsNullOrEmpty(rawExternalId) && rawExternalId.Contains("."))
            {
                var parts = rawExternalId.Split('.');
                try
                {
                    // 还原 D365 流水号
                    var msgB64 = parts[0].Replace("-", "+").Replace("_", "/");
                    var padding = msgB64.Length % 4;
                    if (padding > 0) msgB64 += new string('=', 4 - padding);
                    d365MessageId = new Guid(Convert.FromBase64String(msgB64)).ToString();
                }
                catch { }

                if (parts.Length >= 2)
                {
                    fromNumber = Uri.UnescapeDataString(parts[1]); // 完美拿到发件人号码
                }
            }

            // 3. 这个id定义在/xgatesmschannel.solution/unmanaged/Other/relationships/customizations.xml中
            Guid channelId = Guid.Parse("d78c79f8-d725-41ff-b0de-87988baf2643");

            // 4. 状态映射 (保留您原来的逻辑)
            string status;
            if (string.Equals(state, "DELIVRD", StringComparison.OrdinalIgnoreCase))
                status = "Delivered";
            else if (string.Equals(state, "UNDELIV", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(state, "REJECTED", StringComparison.OrdinalIgnoreCase))
                status = "Failed";
            else
                status = "Sent";

            // 5. 组装最终的上报实体
            var deliveryReport = new DeliveryReport()
            {
                ChannelDefinitionId = channelId,
                MessageId = messageID,
                RequestId = d365MessageId,
                Status = status,
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
    }
}
