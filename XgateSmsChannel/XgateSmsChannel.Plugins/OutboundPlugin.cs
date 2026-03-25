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
        private static readonly HttpClient httpClient = new HttpClient();

        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = serviceProvider.Get<ITracingService>();
            tracingService.Trace("Executing outbound SMS channel plugin");
            var pluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();

            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace(payload);

            var payloadObject = JsonUtils.Deserialize<Payload>(payload);

            // --- Requirement 1: Dynamically fetch account credentials from Dataverse ---
            var serviceFactory = serviceProvider.Get<IOrganizationServiceFactory>();
            var orgService = serviceFactory.CreateOrganizationService(null);

            var accountQuery = new QueryExpression("xgate_xgatesmschannelinstanceaccount")
            {
                TopCount = 1,
                ColumnSet = new ColumnSet("xgate_accountid", "xgate_accountsecret")
            };
            var accountResults = orgService.RetrieveMultiple(accountQuery);

            if (accountResults.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    "未找到 SMS 渠道账号配置，请先在 'Xgate Sms Channel instance account' 表中配置账号。");
            }

            var accountEntity = accountResults.Entities[0];
            var appId = accountEntity.GetAttributeValue<string>("xgate_accountid");
            var appSecret = accountEntity.GetAttributeValue<string>("xgate_accountsecret");
            tracingService.Trace("Account config loaded, appId=" + appId);

            // --- Requirement 2: Build super ExternalId (极限瘦身版：只传 MsgId 和 From) ---
            var msgGuid = string.IsNullOrEmpty(payloadObject.RequestId) ? Guid.NewGuid() : Guid.Parse(payloadObject.RequestId);
            var fromNumber = payloadObject.From;

            // 1. 将流水号 Guid 压缩成 22 位安全字符串
            var msgB64 = Convert.ToBase64String(msgGuid.ToByteArray()).Replace("+", "-").Replace("/", "_").TrimEnd('=');

            // 2. 终极格式：流水号(22) + "."(1) + 发件人(最多20) = 绝对不超过 43 字符！
            var superExternalId = $"{msgB64}.{fromNumber}";
            tracingService.Trace("msgGuid=" + msgGuid + ", from=" + fromNumber + ", superExternalId=" + superExternalId);

            var status = "Failed";
            var messageId = msgGuid.ToString();

            try
            {
                // Step 1: Obtain access token
                var tokenRequestBody = JsonUtils.Serialize(new TokenRequest
                {
                    AppId = appId,
                    AppSecret = appSecret
                });
                tracingService.Trace("Token request: " + tokenRequestBody);

                var tokenHttpResponse = httpClient.PostAsync(
                    "https://sms-api-uat.xgate.com/sms/2.0/token",
                    new StringContent(tokenRequestBody, Encoding.UTF8, "application/json")
                ).GetAwaiter().GetResult();

                var tokenResponseBody = tokenHttpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                tracingService.Trace("Token response: " + tokenResponseBody);
                tokenHttpResponse.EnsureSuccessStatusCode();

                var tokenResponse = JsonUtils.Deserialize<TokenResponse>(tokenResponseBody);
                var accessToken = tokenResponse.AccessToken;
                tracingService.Trace("AccessToken obtained successfully");

                // --- Requirement 3: Assemble Xgate-specific SMS request body ---
                var smsRequestBody = JsonUtils.Serialize(new SmsRequest
                {
                    MessageBody = payloadObject.Message.ContainsKey("text")
                        ? payloadObject.Message["text"]
                        : payloadObject.Message.Values.FirstOrDefault() ?? string.Empty,
                    ToList = new List<SmsRecipient>
                    {
                        new SmsRecipient
                        {
                            To = payloadObject.To,
                            ExternalId = superExternalId
                        }
                    }
                });
                tracingService.Trace("SMS request: " + smsRequestBody);

                var smsHttpRequest = new HttpRequestMessage(HttpMethod.Post, "https://sms-api-uat.xgate.com/sms/2.0/send")
                {
                    Content = new StringContent(smsRequestBody, Encoding.UTF8, "application/json")
                };
                smsHttpRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var smsHttpResponse = httpClient.SendAsync(smsHttpRequest).GetAwaiter().GetResult();
                var smsResponseBody = smsHttpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                tracingService.Trace("SMS response: " + smsResponseBody);
                smsHttpResponse.EnsureSuccessStatusCode();

                // Step 3: Parse send result
                var smsResponse = JsonUtils.Deserialize<SmsResponse>(smsResponseBody);
                if (smsResponse.CountOfStatus != null
                    && smsResponse.CountOfStatus.Success == 1
                    && smsResponse.ReceiveInfo != null
                    && smsResponse.ReceiveInfo.Count > 0)
                {
                    messageId = smsResponse.ReceiveInfo[0].MessageId;
                    status = "Sent";
                    tracingService.Trace("SMS sent successfully. MessageId: " + messageId);
                }
                else
                {
                    tracingService.Trace("SMS send did not return success status");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("Error sending SMS: " + ex.ToString());
            }

            var responseObject = new Response()
            {
                ChannelDefinitionId = payloadObject.ChannelDefinitionId,
                MessageId = messageId,
                RequestId = payloadObject.RequestId,
                Status = status,
                StatusDetails = null
            };

            pluginExecutionContext.OutputParameters["response"] = JsonUtils.Serialize(responseObject);
        }
    }
}
