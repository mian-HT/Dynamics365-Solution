namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;

    public class GetTemplatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            try
            {
                // 1. 从 payload JSON 中解析出 templateId
                string payload = (string)context.InputParameters["payload"];
                tracingService.Trace($"Input payload: {payload}");
                var payloadObj = JsonUtils.Deserialize<GetTemplateRequest>(payload);
                string templateId = payloadObj.templateId;
                tracingService.Trace($"Start fetching template details for ID: {templateId}");

                // 2. 从 xgate_whatsappchannelinstance 表中获取鉴权 Token
                string token = this.GetAuthToken(organizationService, tracingService);
                
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
                context.OutputParameters["response"] = responseBody;
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
                context.OutputParameters["response"] = $"{{\"code\": -1, \"message\": \"{errorDetail.Replace("\"", "\\\"")}\"}}";
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Error: {ex.Message}");
                context.OutputParameters["response"] = $"{{\"code\": -1, \"message\": \"{ex.Message.Replace("\"", "\\\"")}\"}}";
            }
        }

        [DataContract]
        private class GetTemplateRequest
        {
            [DataMember]
            public string templateId { get; set; }
        }

        private string GetAuthToken(IOrganizationService organizationService, ITracingService tracingService)
        {
            var channelInstances = organizationService.RetrieveMultiple(new QueryExpression("msdyn_channelinstance")
            {
                ColumnSet = new ColumnSet("msdyn_extendedentityid"),
                TopCount = 1
            });

            if (channelInstances.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException("No channel instance found.");
            }

            var extendedEntityRef = channelInstances.Entities[0].GetAttributeValue<EntityReference>("msdyn_extendedentityid");
            var whatsappChannelInstance = organizationService.Retrieve(extendedEntityRef.LogicalName, extendedEntityRef.Id, new ColumnSet("xgate_authtoken"));
            var token = whatsappChannelInstance.GetAttributeValue<string>("xgate_authtoken");

            tracingService.Trace($"Retrieved auth token from channel instance: {extendedEntityRef.Id}");

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidPluginExecutionException("Auth token is empty in channel instance.");
            }

            return token;
        }
    }
}