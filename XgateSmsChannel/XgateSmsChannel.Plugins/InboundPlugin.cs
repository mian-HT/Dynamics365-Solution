namespace XgateSmsChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;

    using System;

    // Sample inbound plugin. Entire payload is just proxied to the Channel Definition Inbound API
    public class InboundPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing inbound plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();
            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);
            var organizationService = serviceProvider.Get<IOrganizationServiceFactory>().CreateOrganizationService(null);

            var response = organizationService.Execute(new OrganizationRequest("msdyn_D365ChannelsInbound")
            {
                Parameters = {
                    {"inboundPayLoad", payload}
                }
            });

            pluginExecutionContext.OutputParameters["response"] = response.Results["responseMessage"];
        }
    }
}
