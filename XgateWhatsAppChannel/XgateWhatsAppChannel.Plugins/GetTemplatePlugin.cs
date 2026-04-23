namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    public class GetTemplatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

             var pluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace("Input Payload: " + payload);

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            // 解析 D365 传进来的原始 Payload
            var payloadObject = JsonUtils.Deserialize<Payload>(payload);

            try
            {
                // 1. 获取前端 PCF 传来的输入参数
                string templateId = (string)context.InputParameters["TemplateId"];
                tracingService.Trace($"Start fetching template details for ID: {templateId}");

                // 2. 获取鉴权 Token 
                string token = this.GetCredentials(organizationService, payloadObject.ChannelDefinitionId, payloadObject.From);
                
                // 3. 构建请求你们真实接口的 URL (请根据你们实际的 GET/POST 接口调整)
                string apiUrl = $"https://xcrm360-api-uat.xgatecorp.com/whatsapp/openapi/template/detail?templateId={templateId}";
                var request = WebRequest.CreateHttp(apiUrl);
                request.Method = "GET"; 
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");

                // 4. 发起请求并读取返回结果
                string responseBody = "";
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    responseBody = streamReader.ReadToEnd();
                }

                tracingService.Trace($"Xgate API Response: {responseBody}");

                // 5. 将完整的 JSON 字符串输出给前端 PCF
                context.OutputParameters["TemplateDataJson"] = responseBody;
            }
            catch (WebException webEx)
            {
                // 捕获 HTTP 请求异常
                string errorDetail = webEx.Message;
                if (webEx.Response != null)
                {
                    using (var streamReader = new StreamReader(webEx.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        errorDetail = streamReader.ReadToEnd();
                    }
                }
                tracingService.Trace($"Web Error: {errorDetail}");
                // 返回一个友好的错误 JSON 格式给前端
                context.OutputParameters["TemplateDataJson"] = $"{{\"code\": -1, \"message\": \"{errorDetail.Replace("\"", "\\\"")}\"}}";
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Error: {ex.Message}");
                context.OutputParameters["TemplateDataJson"] = $"{{\"code\": -1, \"message\": \"{ex.Message.Replace("\"", "\\\"")}\"}}";
            }
        }

         private String GetCredentials(IOrganizationService organizationService, Guid channelDefinitionId, string from)
        {
            var channelInstances = organizationService.RetrieveMultiple(new QueryExpression("msdyn_channelinstance")
            {
                ColumnSet = new ColumnSet("msdyn_extendedentityid"),
                Criteria = new FilterExpression()
                {
                    Conditions = {
                        new ConditionExpression("msdyn_channeldefinitionid", ConditionOperator.Equal, channelDefinitionId),
                        new ConditionExpression("msdyn_contactpoint", ConditionOperator.Equal, from)
                    }
                }
            });

            var extendedChannelInstance = channelInstances.Entities[0].GetAttributeValue<EntityReference>("msdyn_extendedentityid");

            var whatsappChannelInstance = organizationService.Retrieve(extendedChannelInstance.LogicalName, extendedChannelInstance.Id, new ColumnSet("xgate_accountid", "xgate_authtoken"));
            var xgateAccountId = whatsappChannelInstance.GetAttributeValue<string>("xgate_accountid");
            var xgateAuthtoken = whatsappChannelInstance.GetAttributeValue<string>("xgate_authtoken");

            return xgateAuthtoken;
        }
    }
}