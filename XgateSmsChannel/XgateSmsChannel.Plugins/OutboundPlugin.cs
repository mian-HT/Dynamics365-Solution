namespace XgateSmsChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Extensions;
    using Microsoft.Xrm.Sdk.Query;
    using XgateSmsChannel.Plugins.ChannelContracts;
    using XgateSmsChannel.Plugins.XgateContracts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    public class OutboundPlugin : IPlugin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell", "S1075:URIs should not be hardcoded",
            Justification = "网关默认基地址，仅在账号未配置 ApiBaseUrl 时作为兜底使用")]
        private const string DefaultBaseUrl = "https://sms-api.xgate.com/sms/2.0";
        private const string DefaultFrom = "10690000";
        private const string AccountEntityName = "xgate_xgatesmschannelinstanceaccount";

        // D365 运行时所有插件实例共享同一个 HttpClient，避免 socket 耗尽
        private static readonly HttpClient SharedHttpClient = new HttpClient();

        private readonly HttpClient httpClient;

        public OutboundPlugin()
        {
            this.httpClient = SharedHttpClient;
        }

        // 供单元测试注入自定义 HttpMessageHandler 以模拟网关响应
        public OutboundPlugin(HttpMessageHandler httpMessageHandler)
        {
            this.httpClient = new HttpClient(httpMessageHandler);
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing outbound SMS channel plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();

            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);

            var payloadObject = JsonUtils.Deserialize<Payload>(payload);

            var status = "Failed"; // 默认失败，只有成功走到最后才会变为 Sent
            var requestId = string.IsNullOrEmpty(payloadObject.RequestId) ? Guid.NewGuid().ToString() : payloadObject.RequestId;
            var messageId = requestId;
            var statusDetails = new Dictionary<string, object>();

            try
            {
                var rawFrom = payloadObject.From ?? DefaultFrom;
                tracingService.Trace($"用户选择的发件人: {rawFrom}, 所属账号ID: {payloadObject.ChannelInstanceId}");

                var orgService = serviceProvider.Get<IOrganizationServiceFactory>().CreateOrganizationService(null);

                var accountEntity = ResolveAccountEntity(orgService, payloadObject, rawFrom, tracingService);
                ValidateAccount(accountEntity, rawFrom);

                var appId = accountEntity.GetAttributeValue<string>("xgate_accountid");
                var appSecret = accountEntity.GetAttributeValue<string>("xgate_accountsecret");
                var baseUrl = ResolveBaseUrl(accountEntity);

                messageId = SendSms(baseUrl, appId, appSecret, payloadObject, rawFrom, pluginExecutionContext.OrganizationId.ToString(), requestId, tracingService);
                status = "Sent";
            }
            catch (Exception ex)
            {
                // 终极兜底：所有异常都会流向这里，确保插件不崩溃，而是把错误信息装进 StatusDetails
                status = "Failed";
                statusDetails.Add("ErrorDetails", ex.Message);
                tracingService.Trace("🚨 短信发送被拦截/发生异常: " + ex.ToString());
            }

            var responseObject = new Response()
            {
                ChannelDefinitionId = payloadObject.ChannelDefinitionId,
                MessageId = messageId,
                RequestId = payloadObject.RequestId,
                Status = status,
                StatusDetails = statusDetails.Count > 0 ? statusDetails : null
            };

            pluginExecutionContext.OutputParameters["response"] = JsonUtils.Serialize(responseObject);
        }

        // 先按主键直查，查不到再走标准 CI-J 三表联动兜底查询
        private static Entity ResolveAccountEntity(IOrganizationService orgService, Payload payloadObject, string rawFrom, ITracingService tracingService)
        {
            Entity accountEntity = null;

            if (IsConcreteInstanceId(payloadObject.ChannelInstanceId))
            {
                tracingService.Trace("检测到完整 InstanceId，进入主键直查...");
                try
                {
                    accountEntity = orgService.Retrieve(
                        AccountEntityName,
                        Guid.Parse(payloadObject.ChannelInstanceId),
                        new ColumnSet("xgate_accountid", "xgate_accountsecret", "xgate_apibaseurl", "statecode"));
                }
                catch (Exception ex)
                {
                    // 查不到则静默降级，交给下面的兜底通道处理
                    tracingService.Trace("主键直查失败，转兜底查询: " + ex.Message);
                }
            }

            return accountEntity ?? QueryAccountByContactPoint(orgService, rawFrom, tracingService);
        }

        private static bool IsConcreteInstanceId(string channelInstanceId)
        {
            return !string.IsNullOrEmpty(channelInstanceId)
                && channelInstanceId != Guid.Empty.ToString()
                && channelInstanceId.Replace("-", "") != "00000000000000000000000000000000";
        }

        // 标准 CI-J 链路查询：自定义配置表 -> msdyn_channelinstanceaccount -> msdyn_channelinstance，按发件人号码匹配
        private static Entity QueryAccountByContactPoint(IOrganizationService orgService, string rawFrom, ITracingService tracingService)
        {
            tracingService.Trace($"通道 2：启动标准 CI-J 链路查询，From: {rawFrom}");

            var fallbackQuery = new QueryExpression(AccountEntityName)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet("xgate_accountid", "xgate_accountsecret", "xgate_apibaseurl", "statecode")
            };
            fallbackQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var accountLink = fallbackQuery.AddLink(
                "msdyn_channelinstanceaccount",
                "xgate_xgatesmschannelinstanceaccountid",
                "msdyn_extendedentityid",
                JoinOperator.Inner);

            var instanceLink = accountLink.AddLink(
                "msdyn_channelinstance",
                "msdyn_channelinstanceaccountid",
                "msdyn_channelinstanceaccountid",
                JoinOperator.Inner);

            instanceLink.LinkCriteria.AddCondition("msdyn_contactpoint", ConditionOperator.Equal, rawFrom);

            var fallbackResults = orgService.RetrieveMultiple(fallbackQuery);
            if (fallbackResults.Entities.Count > 0)
            {
                var entity = fallbackResults.Entities[0];
                tracingService.Trace($"三表联动查询成功！Id={entity.Id}, AccountId={entity.GetAttributeValue<string>("xgate_accountid")}, ApiBaseUrl={entity.GetAttributeValue<string>("xgate_apibaseurl")}");
                return entity;
            }

            return null;
        }

        private static void ValidateAccount(Entity accountEntity, string rawFrom)
        {
            if (accountEntity == null)
            {
                throw new XgateGatewayException($"配置异常: 未在系统中找到发件人 [{rawFrom}] 对应的启用状态的账号配置 (测试发送或正式发送均匹配失败)。");
            }

            var stateCode = accountEntity.GetAttributeValue<OptionSetValue>("statecode");
            if (stateCode != null && stateCode.Value != 0)
            {
                throw new XgateGatewayException("配置异常: 当前选择的发件人所属的账号已被停用。");
            }
        }

        private static string ResolveBaseUrl(Entity accountEntity)
        {
            var baseUrl = accountEntity.GetAttributeValue<string>("xgate_apibaseurl");
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = DefaultBaseUrl;
            }

            return baseUrl.TrimEnd('/');
        }

        private string SendSms(string baseUrl, string appId, string appSecret, Payload payloadObject, string from, string organizationId, string requestId, ITracingService tracingService)
        {
            var smsRequestBody = JsonUtils.Serialize(new SmsRequest
            {
                OrganizationId = organizationId,
                RequestId = requestId,
                From = from,
                To = payloadObject.To,
                Message = ResolveMessageBody(payloadObject)
            });

            var smsHttpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/send")
            {
                Content = new StringContent(smsRequestBody, Encoding.UTF8, "application/json")
            };

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appId}:{appSecret}"));
            smsHttpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var smsHttpResponse = httpClient.SendAsync(smsHttpRequest).GetAwaiter().GetResult();
            var smsResponseBody = smsHttpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!smsHttpResponse.IsSuccessStatusCode)
            {
                throw new XgateGatewayException($"请求发送接口异常 (HTTP {smsHttpResponse.StatusCode}): {smsResponseBody}");
            }

            var smsResponse = JsonUtils.Deserialize<SmsResponse>(smsResponseBody);

            if (smsResponse.CountOfStatus != null && smsResponse.CountOfStatus.Success == 1
                && smsResponse.ReceiveInfo != null && smsResponse.ReceiveInfo.Count > 0)
            {
                var messageId = smsResponse.ReceiveInfo[0].MessageId;
                tracingService.Trace("SMS sent successfully. MessageId: " + messageId);
                return messageId;
            }

            throw new XgateGatewayException($"网关拒绝发送 (业务异常): {smsResponseBody}");
        }

        private static string ResolveMessageBody(Payload payloadObject)
        {
            return payloadObject.Message.ContainsKey("text")
                ? payloadObject.Message["text"]
                : payloadObject.Message.Values.FirstOrDefault() ?? string.Empty;
        }
    }
}
