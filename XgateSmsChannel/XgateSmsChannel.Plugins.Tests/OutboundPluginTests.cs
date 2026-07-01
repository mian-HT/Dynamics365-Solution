namespace XgateSmsChannel.Plugins.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Moq;
    using Moq.Protected;
    using Xunit;
    using XgateSmsChannel.Plugins;
    using XgateSmsChannel.Plugins.ChannelContracts;

    /// <summary>
    /// OutboundPlugin 端到端单元测试：用 Moq 模拟 D365 插件管线 + Xgate 网关 HTTP。
    /// </summary>
    public class OutboundPluginTests
    {
        private const string BaseUrl = "https://fake-gateway.local/sms/2.0";

        [Fact]
        public void Execute_HappyPath_DirectRetrieve_ReturnsSentWithGatewayMessageId()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            var payload = BuildPayloadJson(channelInstanceId: Guid.NewGuid().ToString(), requestId: Guid.NewGuid().ToString());
            ctx.SetPayload(payload);

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-REAL-1\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Sent", response.Status);
            Assert.Equal("MSG-REAL-1", response.MessageId);
            Assert.Null(response.StatusDetails);
        }

        [Fact]
        public void Execute_HappyPath_FallbackLinkQuery_ReturnsSent()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.RetrieveMultiple(It.IsAny<QueryBase>()))
                .Returns(new EntityCollection(new List<Entity> { BuildAccount(stateCode: 0) }));

            ctx.SetPayload(BuildPayloadJson(channelInstanceId: string.Empty, requestId: Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-FB-9\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Sent", response.Status);
            Assert.Equal("MSG-FB-9", response.MessageId);
        }

        [Fact]
        public void Execute_AccountNotFound_ReturnsFailedWithErrorDetails()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.RetrieveMultiple(It.IsAny<QueryBase>()))
                .Returns(new EntityCollection());

            ctx.SetPayload(BuildPayloadJson(channelInstanceId: string.Empty, requestId: Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler();

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Failed", response.Status);
            Assert.NotNull(response.StatusDetails);
            Assert.Contains("ErrorDetails", response.StatusDetails.Keys);
            Assert.Contains("未在系统中找到发件人", response.StatusDetails["ErrorDetails"].ToString());
        }

        [Fact]
        public void Execute_AccountDisabled_ReturnsFailed()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 1));

            ctx.SetPayload(BuildPayloadJson(channelInstanceId: Guid.NewGuid().ToString(), requestId: Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler();

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Failed", response.Status);
            Assert.Contains("已被停用", response.StatusDetails["ErrorDetails"].ToString());
        }

        [Fact]
        public void Execute_SendEndpointHttpError_ReturnsFailed()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            ctx.SetPayload(BuildPayloadJson(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.InternalServerError, sendBody: "boom");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Failed", response.Status);
            Assert.Contains("请求发送接口异常", response.StatusDetails["ErrorDetails"].ToString());
        }

        [Fact]
        public void Execute_SendBusinessFailure_ReturnsFailedWithGatewayBody()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            ctx.SetPayload(BuildPayloadJson(channelInstanceId: Guid.NewGuid().ToString(), requestId: Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":0,\"FAILED\":1},\"ReceiveInfo\":[]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Failed", response.Status);
            Assert.Contains("网关拒绝发送", response.StatusDetails["ErrorDetails"].ToString());
        }

        [Fact]
        public void Constructor_Default_UsesSharedHttpClientWithoutThrowing()
        {
            var plugin = new OutboundPlugin();
            Assert.NotNull(plugin);
        }

        [Fact]
        public void Execute_EmptyBaseUrl_FallsBackToDefaultGatewayUrlAndSends()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0, apiBaseUrl: string.Empty));

            ctx.SetPayload(BuildPayloadJson(channelInstanceId: Guid.NewGuid().ToString(), requestId: Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-DEF-1\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Sent", response.Status);
            Assert.Equal("MSG-DEF-1", response.MessageId);
        }

        [Fact]
        public void Execute_MessageWithoutTextKey_UsesFirstAvailableValue()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            var message = new Dictionary<string, string> { { "body", "no-text-key-here" } };
            ctx.SetPayload(BuildPayloadJson(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), message));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-2\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            Assert.Equal("Sent", ctx.GetResponse().Status);
        }

        [Fact]
        public void Execute_EmptyRequestId_StillSendsSuccessfully()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            ctx.SetPayload(BuildPayloadJson(Guid.NewGuid().ToString(), requestId: string.Empty));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-3\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Sent", response.Status);
            Assert.Equal("MSG-3", response.MessageId);
        }

        [Fact]
        public void Execute_DirectRetrieveThrows_FallsBackToLinkQuery()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Throws(new InvalidOperationException("record not found"));
            ctx.OrgService
                .Setup(s => s.RetrieveMultiple(It.IsAny<QueryBase>()))
                .Returns(new EntityCollection(new List<Entity> { BuildAccount(stateCode: 0) }));

            ctx.SetPayload(BuildPayloadJson(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            var handler = BuildHttpHandler(
                sendStatus: HttpStatusCode.OK, sendBody: "{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-FB-X\"}]}");

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            var response = ctx.GetResponse();
            Assert.Equal("Sent", response.Status);
            Assert.Equal("MSG-FB-X", response.MessageId);
        }

        [Fact]
        public void Execute_SendRequest_UsesBasicAuth()
        {
            var ctx = new PipelineContext();
            ctx.OrgService
                .Setup(s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Returns(BuildAccount(stateCode: 0));

            ctx.SetPayload(BuildPayloadJson(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            HttpRequestMessage capturedRequest = null;
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, _) => capturedRequest = r)
                .Returns(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"CountOfStatus\":{\"SUCCESS\":1,\"FAILED\":0},\"ReceiveInfo\":[{\"MessageId\":\"MSG-BA\"}]}")
                }));

            new OutboundPlugin(handler.Object).Execute(ctx.ServiceProvider.Object);

            Assert.NotNull(capturedRequest);
            Assert.Equal("Basic", capturedRequest.Headers.Authorization.Scheme);
            var decoded = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(capturedRequest.Headers.Authorization.Parameter));
            Assert.Equal("test-app-id:test-app-secret", decoded);
        }

        // ----------------- 测试辅助 -----------------

        private static Entity BuildAccount(int stateCode, string apiBaseUrl = BaseUrl)
        {
            var account = new Entity("xgate_xgatesmschannelinstanceaccount", Guid.NewGuid());
            account["xgate_accountid"] = "test-app-id";
            account["xgate_accountsecret"] = "test-app-secret";
            account["xgate_apibaseurl"] = apiBaseUrl;
            account["statecode"] = new OptionSetValue(stateCode);
            return account;
        }

        private static string BuildPayloadJson(string channelInstanceId, string requestId, Dictionary<string, string> message = null)
        {
            var payload = new Payload
            {
                ChannelDefinitionId = Guid.NewGuid(),
                ChannelInstanceId = channelInstanceId,
                RequestId = requestId,
                From = "10690000",
                To = "85261234567",
                Message = message ?? new Dictionary<string, string> { { "text", "Hello Xgate" } }
            };
            return JsonUtils.Serialize(payload);
        }

        private static Mock<HttpMessageHandler> BuildHttpHandler(
            HttpStatusCode sendStatus = HttpStatusCode.OK, string sendBody = "{}")
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.AbsoluteUri.EndsWith("/send")),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => Task.FromResult(new HttpResponseMessage(sendStatus)
                {
                    Content = new StringContent(sendBody)
                }));

            return handler;
        }

        /// <summary>
        /// 封装一条标准 D365 插件执行管线的所有 mock。
        /// </summary>
        private sealed class PipelineContext
        {
            public Mock<IServiceProvider> ServiceProvider { get; }
            public Mock<IPluginExecutionContext> PluginContext { get; }
            public Mock<IOrganizationService> OrgService { get; }
            public ParameterCollection InputParameters { get; }
            public ParameterCollection OutputParameters { get; }

            public PipelineContext()
            {
                ServiceProvider = new Mock<IServiceProvider>();
                PluginContext = new Mock<IPluginExecutionContext>();
                OrgService = new Mock<IOrganizationService>();
                var tracing = new Mock<ITracingService>();
                var factory = new Mock<IOrganizationServiceFactory>();

                InputParameters = new ParameterCollection();
                OutputParameters = new ParameterCollection();
                PluginContext.Setup(c => c.InputParameters).Returns(InputParameters);
                PluginContext.Setup(c => c.OutputParameters).Returns(OutputParameters);
                PluginContext.Setup(c => c.OrganizationId).Returns(Guid.NewGuid());

                factory.Setup(f => f.CreateOrganizationService(It.IsAny<Guid?>())).Returns(OrgService.Object);

                ServiceProvider.Setup(p => p.GetService(typeof(ITracingService))).Returns(tracing.Object);
                ServiceProvider.Setup(p => p.GetService(typeof(IPluginExecutionContext))).Returns(PluginContext.Object);
                ServiceProvider.Setup(p => p.GetService(typeof(IOrganizationServiceFactory))).Returns(factory.Object);
            }

            public void SetPayload(string payloadJson)
            {
                InputParameters["payload"] = payloadJson;
            }

            public Response GetResponse()
            {
                Assert.True(OutputParameters.Contains("response"), "插件未写入 OutputParameters[\"response\"]");
                return JsonUtils.Deserialize<Response>((string)OutputParameters["response"]);
            }
        }
    }
}
