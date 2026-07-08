namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;

    public class GetTemplateListPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            try
            {
                // 1. 从 payload JSON 中解析出 senderId
                string payload = (string)context.InputParameters["payload"];
                tracingService.Trace($"Input payload: {payload}");
                var payloadObj = JsonUtils.Deserialize<GetTemplateListRequest>(payload);
                string senderId = payloadObj.senderId;
                tracingService.Trace($"Start fetching template list for senderId: {senderId}");

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    throw new InvalidPluginExecutionException("senderId is required to fetch the template list.");
                }

                // 2. 直接从 WhatsApp 渠道实例扩展表获取共用的鉴权 Token
                string token = this.GetAuthToken(organizationService, tracingService);

                // 3. 调用 Xgate 拉取模板列表接口
                string apiUrl = $"https://xcrm360-api-uat.xgatecorp.com/whatsapp/openapi/template/list?senderId={senderId}";
                var request = WebRequest.CreateHttp(apiUrl);
                request.Method = "GET";
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");

                string maskedToken = token.Length > 12 ? $"{token.Substring(0, 6)}...{token.Substring(token.Length - 6)}" : "***";
                tracingService.Trace($"Xgate API Request => Method: {request.Method}, Url: {apiUrl}, senderId: {senderId}, Authorization: Bearer {maskedToken}");

                // 4. 发起请求并读取返回结果
                string responseBody;
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
                string errorDetail = webEx.Message;
                if (webEx.Response != null)
                {
                    using (var streamReader = new StreamReader(webEx.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        errorDetail = streamReader.ReadToEnd();
                    }
                }
                tracingService.Trace($"Web Error: {errorDetail}");
                context.OutputParameters["response"] = $"{{\"code\": -1, \"message\": \"{errorDetail.Replace("\"", "\\\"")}\"}}";
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Error: {ex.Message}");
                context.OutputParameters["response"] = $"{{\"code\": -1, \"message\": \"{ex.Message.Replace("\"", "\\\"")}\"}}";
            }
        }

        [DataContract]
        private class GetTemplateListRequest
        {
            [DataMember]
            public string senderId { get; set; }
        }

        private string GetAuthToken(IOrganizationService organizationService, ITracingService tracingService)
        {
            // 所有 sender 共用同一套 account/token，直接取任意一条带 token 的 WhatsApp 渠道实例
            var result = organizationService.RetrieveMultiple(new QueryExpression("xgate_whatsappchannelinstance")
            {
                ColumnSet = new ColumnSet("xgate_authtoken"),
                TopCount = 1,
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("xgate_authtoken", ConditionOperator.NotNull) }
                }
            });

            if (result.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException("No WhatsApp channel instance with an auth token was found.");
            }

            var token = result.Entities[0].GetAttributeValue<string>("xgate_authtoken");
            tracingService.Trace("Retrieved auth token from xgate_whatsappchannelinstance.");

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidPluginExecutionException("Auth token is empty in channel instance.");
            }

            return token;
        }
    }
}
