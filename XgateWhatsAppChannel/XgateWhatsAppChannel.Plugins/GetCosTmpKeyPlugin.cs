namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// 获取腾讯云 COS 临时密钥（STS）代理插件。
    /// PCF 前端要直传 COS 但不能持有永久密钥，通过本 Custom API 拿临时密钥。
    /// organizationId 取自服务端可信的 context.OrganizationId（前端无法伪造），
    /// 用于凭证接口按组织做 COS 资源隔离（返回的 allowPrefix 形如 whatsapp/{orgId}/*）。
    /// 凭证接口响应体原样透传给前端 PCF。
    /// </summary>
    public class GetCosTmpKeyPlugin : IPlugin
    {
        // 腾讯云 COS 临时密钥接口（UAT），使用 WhatsApp 渠道实例的 xgate_authtoken 做 Bearer 鉴权
        private const string CredentialApiBase = "https://connector-api-uat.xgatecorp.com/crm/report/api/whatsapp/cos/credential";

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            try
            {
                // organizationId 取服务端可信的组织 GUID（前端无法伪造），用于 COS 目录隔离
                string organizationId = context.OrganizationId.ToString();
                tracingService.Trace($"Fetching COS temp key for organizationId: {organizationId}");

                // Bearer token 取自 WhatsApp 渠道实例扩展表（与 GetTemplatePlugin 等其它插件一致）
                string token = this.GetAuthToken(organizationService, tracingService);

                string apiUrl = $"{CredentialApiBase}?organizationId={Uri.EscapeDataString(organizationId)}";
                var request = WebRequest.CreateHttp(apiUrl);
                request.Method = "POST";
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");
                request.ContentLength = 0;

                string responseBody;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    responseBody = streamReader.ReadToEnd();
                }

                tracingService.Trace("COS temp key API responded.");
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
