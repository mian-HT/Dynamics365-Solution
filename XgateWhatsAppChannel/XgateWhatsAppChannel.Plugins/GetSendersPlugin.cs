namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    public class GetSendersPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            try
            {
                // 1. payload 目前无需参数，仅打印以便排查
                string payload = context.InputParameters.Contains("payload") ? (string)context.InputParameters["payload"] : null;
                tracingService.Trace($"Input payload: {payload}");
                tracingService.Trace("Start fetching sender list");

                // 2. 直接从 WhatsApp 渠道实例扩展表获取共用的鉴权 Token
                string token = this.GetAuthToken(organizationService, tracingService);

                // 3. 调用 Xgate 拉取 sender 列表接口
                string apiUrl = "https://xcrm360-api-uat.xgatecorp.com/whatsapp/openapi/sender/list";
                var request = WebRequest.CreateHttp(apiUrl);
                request.Method = "GET";
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");

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
