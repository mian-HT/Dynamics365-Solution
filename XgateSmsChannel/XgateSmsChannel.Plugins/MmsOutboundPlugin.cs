namespace XgateSmsChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using System;
    using System.Collections.Generic;
    using XgateSmsChannel.Plugins.ChannelContracts;

    /// <summary>
    /// Xgate MMS custom channel outbound entry point.
    ///
    /// The channel metadata and Custom API are intentionally deployed before the
    /// provider contract is available.  This implementation never calls an
    /// external endpoint: it returns a structured failure so a draft MMS channel
    /// cannot accidentally send traffic through the SMS gateway.
    /// </summary>
    public class MmsOutboundPlugin : IPlugin
    {
        public const string ProviderNotConfiguredMessage =
            "Xgate MMS provider integration has not been configured yet.";

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();
            var payload = pluginExecutionContext.InputParameters["payload"] as string;

            tracingService.Trace("Executing outbound MMS channel placeholder plugin");
            tracingService.Trace(payload ?? string.Empty);

            var payloadObject = JsonUtils.Deserialize<Payload>(payload);
            var requestId = string.IsNullOrWhiteSpace(payloadObject?.RequestId)
                ? Guid.NewGuid().ToString()
                : payloadObject.RequestId;

            var response = new Response
            {
                ChannelDefinitionId = payloadObject == null
                    ? Guid.Empty
                    : payloadObject.ChannelDefinitionId,
                RequestId = requestId,
                MessageId = requestId,
                Status = "Failed",
                StatusDetails = new Dictionary<string, object>
                {
                    { "ErrorDetails", ProviderNotConfiguredMessage }
                }
            };

            pluginExecutionContext.OutputParameters["response"] = JsonUtils.Serialize(response);
        }
    }
}
