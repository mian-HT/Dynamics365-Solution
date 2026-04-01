namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using Microsoft.Xrm.Sdk.Query;

    using XgateWhatsAppChannel.Plugins.XgateContracts;

    using System;
    using System.Collections.Generic;


    // This is a plugin for sending delivery report to Channel Definition Delivery report API
    // which should be called by a proxy service that processes incoming notifications from serice provider
    public class DeliveryReportPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing delivery report plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();
            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);
            var organizationService = serviceProvider.Get<IOrganizationServiceFactory>().CreateOrganizationService(null);

            var xgateDeliverReport = JsonUtils.Deserialize<XgateDeliveryReport>(payload);

            // Only processing "delivered" status for demo purpose
            if (xgateDeliverReport.MessageStatus == "delivered")
            {
                // Find request id by message id
                var requestId = organizationService.RetrieveMultiple(new QueryExpression("xgate_requestmessagemapping")
                {
                    ColumnSet = new ColumnSet("xgate_requestid"),
                    Criteria = new FilterExpression()
                    {
                        Conditions = { new ConditionExpression("xgate_messageid", ConditionOperator.Equal, xgateDeliverReport.MessageSid) }
                    }
                }).Entities[0].GetAttributeValue<string>("xgate_requestid");

                var deliveryReport = new DeliveryReport()
                {
                    ChannelDefinitionId = Guid.Parse("702c7021-cf32-4fcd-be63-b3373f27906b"),
                    // Xgate specific prefix
                    From = xgateDeliverReport.From.Replace("whatsapp:", ""),
                    MessageId = xgateDeliverReport.MessageSid,
                    RequestId = requestId,
                    Status = "Delivered",
                    OrganizationId = pluginExecutionContext.OrganizationId.ToString(),
                    StatusDetails = new Dictionary<string, object>()
                };

                var notificatonPayload = JsonUtils.Serialize(deliveryReport);
                tracingService.Trace("Notification payload: {0}", notificatonPayload);

                // Execution of Channel Definitions Notification API
                var response = organizationService.Execute(new OrganizationRequest("msdyn_D365ChannelsNotification")
                {
                    Parameters = {
                        { "notificationPayLoad", notificatonPayload }
                    }
                });

                // Using it for debugging purpose
                pluginExecutionContext.OutputParameters["response"] = response.Results["responseMessage"];
            }
        }
    }
}
