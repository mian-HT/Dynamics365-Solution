namespace XgateWhatsAppChannel.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;

    public class OutboundPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("Executing outbound Xgate WhatsApp channel plugin");
            
            var pluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            var payload = pluginExecutionContext.InputParameters["payload"] as string;
            tracingService.Trace("Input Payload: " + payload);

            var organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var organizationService = organizationServiceFactory.CreateOrganizationService(null);

            // 解析 D365 传进来的原始 Payload
            var payloadObject = JsonUtils.Deserialize<Payload>(payload);
            var credentials = this.GetCredentials(organizationService, payloadObject.ChannelDefinitionId, payloadObject.From);

            // 调用内部 API
            var responseString = SendXgateRequest(credentials.Token, payloadObject, tracingService);
            tracingService.Trace("Xgate API Response: " + responseString);

            // 解析内部 API 返回的结果
            var xgateResponse = JsonUtils.Deserialize<XgateResponse>(responseString);
            bool isSuccess = xgateResponse != null && xgateResponse.code == 0;
            
            // 获取 MessageId，如果失败则生成一个临时的 GUID
            string actualMessageId = (isSuccess && xgateResponse.data != null && !string.IsNullOrEmpty(xgateResponse.data.messageId)) 
                                     ? xgateResponse.data.messageId 
                                     : Guid.NewGuid().ToString();

            // 保存映射关系
            organizationService.Create(new Entity("xgate_requestmessagemapping")
            {
                ["xgate_messageid"] = actualMessageId,
                ["xgate_requestid"] = payloadObject.RequestId
            });

            // 构造返回给 Marketing 引擎的结果
            var responseObject = new Response()
            {
                ChannelDefinitionId = payloadObject.ChannelDefinitionId,
                MessageId = actualMessageId,
                RequestId = payloadObject.RequestId,
                Status = isSuccess ? "Sent" : "SendingFailed",
                Details = new ResponseDetails
                {
                    Message = isSuccess ? "Message was sent successfully via Xgate API" : $"Failed to send message. Error: {xgateResponse?.message}"
                }
            };

            pluginExecutionContext.OutputParameters["response"] = JsonUtils.Serialize(responseObject);
        }

        private Credentials GetCredentials(IOrganizationService organizationService, Guid channelDefinitionId, string from)
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

            return new Credentials
            {
                AccountId = xgateAccountId,
                Token = xgateAuthtoken,
            };
        }

        private static string SendXgateRequest(string token, Payload payloadObject, ITracingService tracingService)
        {
            var request = WebRequest.CreateHttp("https://xcrm360-api-uat.xgatecorp.com/whatsapp/openapi/message/send");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token}");

            payloadObject.Message.TryGetValue("xgate_templateid", out string partTemplateId);
            payloadObject.Message.TryGetValue("xgate_headervariables", out string rawHeader);
            payloadObject.Message.TryGetValue("xgate_bodyvariables", out string rawBody);

            // --- 解析 body：PCF 输出的复合 JSON { senderId, templateId, variables }，并兼容旧格式 ---
            ResolveBody(rawBody, out string senderIdFromBody, out string templateIdFromBody, out string varsJson);
            // senderId / templateId 优先取 PCF 里的值，取不到再回退到 message part
            string senderId = !string.IsNullOrWhiteSpace(senderIdFromBody) ? senderIdFromBody : payloadObject.From;
            string templateIdStr = !string.IsNullOrWhiteSpace(templateIdFromBody) ? templateIdFromBody : partTemplateId;

            int templateId = 0;
            if (!string.IsNullOrEmpty(templateIdStr))
            {
                int.TryParse(templateIdStr, out templateId);
            }

            // header：优先取 PCF 复合 JSON 里的 header 对象（{format, image:{url}} 或 {format:"text", text:[...]}），
            // 取不到再回退旧的 message part 文本
            string compositeBody = (rawBody ?? "").Trim();
            string headerFromBody = ExtractRawJsonValue(compositeBody, "header");
            string headerJson = !string.IsNullOrWhiteSpace(headerFromBody) ? headerFromBody : BuildHeaderJson(rawHeader);

            // 如果 header 有值，加上外层的 key 和逗号
            string headerPart = string.IsNullOrEmpty(headerJson) ? "" : $"\"header\": {headerJson},";

            // buttons：PCF 复合 JSON 里的 buttons 数组（位置数组）
            string buttonsJson = ExtractRawJsonValue(compositeBody, "buttons");
            string buttonsPart = string.IsNullOrEmpty(buttonsJson) ? "" : $",\"buttons\": {buttonsJson}";

            // 使用字符串插值构建最终的 Payload，彻底绕开 D365 沙盒禁止匿名类型序列化的限制
            string postData = $@"{{
                ""senderId"": {long.Parse(senderId)},
                ""receiverPhoneNumber"": ""{payloadObject.To}"",
                ""message"": {{
                    ""type"": ""whatsapp_template"",
                    ""templateId"": {templateId},
                    ""templateParams"": {{
                        {headerPart}
                        ""body"": {{
                            ""variables"": {varsJson}
                        }}{buttonsPart}
                    }}
                }}
            }}";

            tracingService.Trace("Xgate API Request Body: " + postData);
            var messageBytes = Encoding.UTF8.GetBytes(postData);

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(messageBytes, 0, messageBytes.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException exception)
            {
                if (exception.Response != null)
                {
                    using (var response = (HttpWebResponse)exception.Response)
                    using (var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        var content = streamReader.ReadToEnd();
                        tracingService.Trace("Failed to call Xgate API with: {0}", content);
                        return content; 
                    }
                }
                throw;
            }
        }

        // --- body 解析：兼容 PCF 复合 JSON / 旧变量 JSON / 旧 Key:Value 文本 ---

        private static void ResolveBody(string rawBody, out string senderId, out string templateId, out string varsJson)
        {
            senderId = null;
            templateId = null;
            string trimmed = (rawBody ?? "").Trim();

            if (trimmed.StartsWith("{"))
            {
                // 优先尝试解析为复合结构 { "senderId": "...", "templateId": "...", "variables": { ... } }
                try
                {
                    var parsed = JsonUtils.Deserialize<BodyVariablesPayload>(trimmed);
                    if (parsed != null && (parsed.variables != null || !string.IsNullOrEmpty(parsed.senderId) || !string.IsNullOrEmpty(parsed.templateId)))
                    {
                        senderId = parsed.senderId;
                        templateId = parsed.templateId;
                        varsJson = parsed.variables != null ? SerializeVariablesDict(parsed.variables) : "{}";
                        return;
                    }
                }
                catch
                {
                    // 解析失败则走下面的兜底逻辑
                }

                // 旧格式：字段里直接就是变量 JSON 对象 { "k": "v" }
                varsJson = trimmed;
                return;
            }

            // 最旧格式：Key:Value 文本
            varsJson = BuildVariablesJson(rawBody) ?? "{}";
        }

        // 从复合 JSON 字符串里按顶层 key 抠出一段原始 JSON（对象 {...} 或数组 [...]）。
        // 只匹配真正的 key（前一个非空白字符是 { 或 ,），并做括号配平 + 字符串转义处理。
        private static string ExtractRawJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string token = "\"" + key + "\"";
            int searchFrom = 0;

            while (true)
            {
                int keyIdx = json.IndexOf(token, searchFrom, StringComparison.Ordinal);
                if (keyIdx < 0) return null;

                // 校验它是一个真正的 key：前一个非空白字符必须是 { 或 ,
                int p = keyIdx - 1;
                while (p >= 0 && char.IsWhiteSpace(json[p])) p--;
                if (p < 0 || (json[p] != '{' && json[p] != ','))
                {
                    searchFrom = keyIdx + token.Length;
                    continue;
                }

                int i = keyIdx + token.Length;
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length || json[i] != ':')
                {
                    searchFrom = keyIdx + token.Length;
                    continue;
                }
                i++;
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length) return null;

                char c = json[i];
                if (c != '{' && c != '[') return null; // 只处理对象/数组

                char open = c;
                char close = c == '{' ? '}' : ']';
                int depth = 0;
                bool inStr = false;
                bool esc = false;
                int start = i;

                for (; i < json.Length; i++)
                {
                    char ch = json[i];
                    if (inStr)
                    {
                        if (esc) esc = false;
                        else if (ch == '\\') esc = true;
                        else if (ch == '"') inStr = false;
                    }
                    else
                    {
                        if (ch == '"') inStr = true;
                        else if (ch == open) depth++;
                        else if (ch == close)
                        {
                            depth--;
                            if (depth == 0) return json.Substring(start, i - start + 1);
                        }
                    }
                }

                return null;
            }
        }

        private static string SerializeVariablesDict(IDictionary<string, string> dict)
        {
            var parts = new List<string>();
            foreach (var kv in dict)
            {
                string value = (kv.Value ?? string.Empty).Replace("\"", "\\\"");
                parts.Add($"\"{kv.Key}\":\"{value}\"");
            }

            return "{" + string.Join(",", parts) + "}";
        }

        // --- 纯字符串 JSON 构建辅助方法 ---

        private static string BuildHeaderJson(string rawHeader)
        {
            if (string.IsNullOrWhiteSpace(rawHeader)) return "";
            
            string trimmed = rawHeader.Trim();
            // 如果用户输入的是 {"image":{"url":"..."}}，说明已经是 JSON，直接返回
            if (trimmed.StartsWith("{"))
            {
                return trimmed;
            }
            
            // 否则按照 Key:Value 文本解析为 JSON
            return BuildVariablesJson(rawHeader);
        }

        private static string BuildVariablesJson(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return null;

            var jsonParts = new List<string>();
            using (StringReader reader = new StringReader(rawInput))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex > 0)
                    {
                        string key = line.Substring(0, separatorIndex).Trim().Trim('"');
                        string value = line.Substring(separatorIndex + 1).Trim().Trim('"', ',');
                        
                        // 防止变量内部包含双引号导致 JSON 结构破坏
                        value = value.Replace("\"", "\\\""); 
                        
                        jsonParts.Add($"\"{key}\":\"{value}\"");
                    }
                }
            }
            
            return "{" + string.Join(",", jsonParts) + "}";
        }

        // --- 内部数据结构类 ---

        [DataContract]
        private class BodyVariablesPayload
        {
            [DataMember]
            public string senderId { get; set; }

            [DataMember]
            public string templateId { get; set; }

            [DataMember]
            public Dictionary<string, string> variables { get; set; }
        }

        private class Credentials
        {
            public string AccountId { get; set; }
            public string Token { get; set; }
        }

        // 接收的返回值是标准类，D365 的反序列化是可以正常工作的
        public class XgateResponse
        {
            public int code { get; set; }
            public string message { get; set; }
            public XgateResponseData data { get; set; }
        }

        public class XgateResponseData
        {
            public string messageId { get; set; }
        }
    }
}