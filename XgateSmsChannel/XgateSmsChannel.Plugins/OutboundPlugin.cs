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

            // --- 核心改动：提前初始化状态变量，准备随时接住异常 ---
            var status = "Failed"; // 默认状态为失败，只有成功走到最后才会变为 Sent
            var msgGuid = string.IsNullOrEmpty(payloadObject.RequestId) ? Guid.NewGuid() : Guid.Parse(payloadObject.RequestId);
            var messageId = msgGuid.ToString();
            var statusDetails = new Dictionary<string, object>(); // 专门用来装载给前端展示的错误信息

            try
            {
                var rawFrom = payloadObject.From ?? "10690000";
                tracingService.Trace($"用户选择的发件人: {rawFrom}, 所属账号ID: {payloadObject.ChannelInstanceId}");

                var serviceFactory = serviceProvider.Get<IOrganizationServiceFactory>();
                var orgService = serviceFactory.CreateOrganizationService(null);

                Entity accountEntity = null;

                // 按主键直查 ChannelInstanceId存在就用ChannelInstanceId查找
                if (!string.IsNullOrEmpty(payloadObject.ChannelInstanceId) && 
                    payloadObject.ChannelInstanceId != Guid.Empty.ToString() && 
                    payloadObject.ChannelInstanceId.Replace("-", "") != "00000000000000000000000000000000")
                {
                    tracingService.Trace("检测到完整 InstanceId，进入主键直查...");
                    try
                    {
                        accountEntity = orgService.Retrieve(
                            "xgate_xgatesmschannelinstanceaccount", 
                            Guid.Parse(payloadObject.ChannelInstanceId), 
                            new ColumnSet("xgate_accountid", "xgate_accountsecret", "xgate_apibaseurl", "statecode")
                        );
                    }
                    catch { /* 如果查不到，静默失败，交给下面的兜底通道处理 */ }
                }

                // (关联查询)
                if (accountEntity == null)
                {
                    tracingService.Trace($"通道 2：启动标准 CI-J 链路查询，From: {rawFrom}");

                    // 1. 起点：您的自定义配置表 (获取 API Secret/URL)
                    var fallbackQuery = new QueryExpression("xgate_xgatesmschannelinstanceaccount")
                    {
                        TopCount = 1,
                        ColumnSet = new ColumnSet("xgate_accountid", "xgate_accountsecret", "xgate_apibaseurl", "statecode")
                    };
                    fallbackQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

                    // 2. 第一级关联：自定义配置表 <-> 标准账户表 (msdyn_channelinstanceaccount)
                    // 关键点：标准表通过 msdyn_extendedentityid 字段存储了您自定义记录的 GUID 字符串
                    var accountLink = fallbackQuery.AddLink(
                        "msdyn_channelinstanceaccount", 
                        "xgate_xgatesmschannelinstanceaccountid", 
                        "msdyn_extendedentityid", 
                        JoinOperator.Inner
                    );

                    // 3. 第二级关联：标准账户表 <-> 标准实例表 (msdyn_channelinstance)
                    var instanceLink = accountLink.AddLink(
                        "msdyn_channelinstance", 
                        "msdyn_channelinstanceaccountid", // 账户表主键
                        "msdyn_channelinstanceaccountid", // 实例表外键
                        JoinOperator.Inner
                    );

                    // 4. 终点：匹配发件人名称 msdyn_name = rawFrom
                    instanceLink.LinkCriteria.AddCondition("msdyn_name", ConditionOperator.Equal, rawFrom);

                    var fallbackResults = orgService.RetrieveMultiple(fallbackQuery);
                    if (fallbackResults.Entities.Count > 0)
                    {
                        accountEntity = fallbackResults.Entities[0];
                        tracingService.Trace($"三表联动查询成功！已找到对应环境配置。Id={accountEntity.Id}, AccountId={accountEntity.GetAttributeValue<string>("xgate_accountid")}, ApiBaseUrl={accountEntity.GetAttributeValue<string>("xgate_apibaseurl")}");
                    }
                }

                // ==========================================
                // 最终校验与赋值
                // ==========================================
                if (accountEntity == null)
                {
                    throw new Exception($"配置异常: 未在系统中找到发件人 [{rawFrom}] 对应的启用状态的账号配置 (测试发送或正式发送均匹配失败)。");
                }

                var stateCode = accountEntity.GetAttributeValue<OptionSetValue>("statecode");
                if (stateCode != null && stateCode.Value != 0) 
                {
                    throw new Exception($"配置异常: 当前选择的发件人所属的账号已被停用。");
                }

                var appId = accountEntity.GetAttributeValue<string>("xgate_accountid");
                var appSecret = accountEntity.GetAttributeValue<string>("xgate_accountsecret");

                var baseUrl = accountEntity.GetAttributeValue<string>("xgate_apibaseurl");
                if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "https://sms-api.xgate.com/sms/2.0";
                baseUrl = baseUrl.TrimEnd('/'); 

                // xgate sms接口中的ExternalId有长度限制最大64个字符，所以这里压缩字符长度
                var safeFromNumber = Uri.EscapeDataString(rawFrom); 
                var msgB64 = Convert.ToBase64String(msgGuid.ToByteArray()).Replace("+", "-").Replace("/", "_").         TrimEnd('=');
                var superExternalId = $"{msgB64}.{safeFromNumber}";

                // 3：动态拼接 Token 接口地址
                var tokenRequestBody = JsonUtils.Serialize(new TokenRequest { AppId = appId, AppSecret = appSecret });
                var tokenHttpResponse = httpClient.PostAsync(
                    $"{baseUrl}/token", // 动态 URL！
                    new StringContent(tokenRequestBody, Encoding.UTF8, "application/json")
                ).GetAwaiter().GetResult();

                var tokenResponseBody = tokenHttpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!tokenHttpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"登录网关失败 (HTTP {tokenHttpResponse.StatusCode}): {tokenResponseBody}");
                }

                var tokenResponse = JsonUtils.Deserialize<TokenResponse>(tokenResponseBody);
                var accessToken = tokenResponse.AccessToken;

                // Step 2: 组装并发送短信
                var smsRequestBody = JsonUtils.Serialize(new SmsRequest
                {
                    MessageBody = payloadObject.Message.ContainsKey("text")
                        ? payloadObject.Message["text"]
                        : payloadObject.Message.Values.FirstOrDefault() ?? string.Empty,
                    ToList = new List<SmsRecipient>
                    {
                        new SmsRecipient { To = payloadObject.To, ExternalId = superExternalId }
                    }
                });

                //4：动态拼接 Send 接口地址
                var smsHttpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/send") // 动态 URL！
                {
                    Content = new StringContent(smsRequestBody, Encoding.UTF8, "application/json")
                };
                smsHttpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var smsHttpResponse = httpClient.SendAsync(smsHttpRequest).GetAwaiter().GetResult();
                var smsResponseBody = smsHttpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                
                // 【兜底 2】如果发送接口挂了报 500、404，优雅拦截！
                if (!smsHttpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"请求发送接口异常 (HTTP {smsHttpResponse.StatusCode}): {smsResponseBody}");
                }

                // Step 3: 解析真实的网关业务回执
                var smsResponse = JsonUtils.Deserialize<SmsResponse>(smsResponseBody);
                
                // 【兜底 3】HTTP 是 200，但 Xgate 告诉你业务失败（如：欠费、空号、内容违规）
                if (smsResponse.CountOfStatus != null && smsResponse.CountOfStatus.Success == 1 && smsResponse.ReceiveInfo != null && smsResponse.ReceiveInfo.Count > 0)
                {
                    messageId = smsResponse.ReceiveInfo[0].MessageId; // 成功！拿到真 ID
                    status = "Sent"; // 只有在这里，状态才真正变为 Sent
                    tracingService.Trace("SMS sent successfully. MessageId: " + messageId);
                }
                else
                {
                    // 把 Xgate 返回的真实 JSON 错误信息暴露出去，让业务员看到是欠费还是拒收
                    throw new Exception($"网关拒绝发送 (业务异常): {smsResponseBody}");
                }
            }
            catch (Exception ex)
            {
                // 🔥 终极兜底：所有异常都会流向这里！
                // 确保插件不崩溃，而是把报错信息装进 StatusDetails 字典
                status = "Failed";
                statusDetails.Add("ErrorDetails", ex.Message);
                
                // 将错误写入系统日志，方便开发人员后台排查
                tracingService.Trace("🚨 短信发送被拦截/发生异常: " + ex.ToString());
            }

            // --- 组装最终响应 ---
            var responseObject = new Response()
            {
                ChannelDefinitionId = payloadObject.ChannelDefinitionId,
                MessageId = messageId,
                RequestId = payloadObject.RequestId,
                Status = status,
                // 如果 statusDetails 里有数据（报错了），就一并交还给 D365
                StatusDetails = statusDetails.Count > 0 ? statusDetails : null 
            };

            pluginExecutionContext.OutputParameters["response"] = JsonUtils.Serialize(responseObject);
        }
    }
}