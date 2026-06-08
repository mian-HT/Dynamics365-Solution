namespace XgateSmsChannel.Plugins.Tests
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Moq;
    using Xunit;

    /// <summary>
    /// 演示如何用 Moq 搭建 D365 插件执行管线的脚手架，
    /// 后续可基于此为 OutboundPlugin / InboundPlugin 等编写完整单元测试。
    /// </summary>
    public class PluginContextMockTests
    {
        [Fact]
        public void ServiceProvider_ResolvesPluginPipelineServices()
        {
            // Arrange：构造标准插件管线的各个 mock
            var tracingService = new Mock<ITracingService>();
            var pluginContext = new Mock<IPluginExecutionContext>();
            var serviceFactory = new Mock<IOrganizationServiceFactory>();
            var orgService = new Mock<IOrganizationService>();
            var serviceProvider = new Mock<IServiceProvider>();

            var inputParameters = new ParameterCollection { { "payload", "{\"To\":\"85261234567\"}" } };
            var outputParameters = new ParameterCollection();
            pluginContext.Setup(c => c.InputParameters).Returns(inputParameters);
            pluginContext.Setup(c => c.OutputParameters).Returns(outputParameters);

            serviceFactory
                .Setup(f => f.CreateOrganizationService(It.IsAny<Guid?>()))
                .Returns(orgService.Object);

            serviceProvider
                .Setup(p => p.GetService(typeof(ITracingService)))
                .Returns(tracingService.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(IPluginExecutionContext)))
                .Returns(pluginContext.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(IOrganizationServiceFactory)))
                .Returns(serviceFactory.Object);

            // Act：模拟插件内部解析服务的方式
            var resolvedContext =
                (IPluginExecutionContext)serviceProvider.Object.GetService(typeof(IPluginExecutionContext));
            var resolvedFactory =
                (IOrganizationServiceFactory)serviceProvider.Object.GetService(typeof(IOrganizationServiceFactory));
            var resolvedService = resolvedFactory.CreateOrganizationService(null);

            // Assert：验证管线接线正确
            Assert.Same(pluginContext.Object, resolvedContext);
            Assert.Same(orgService.Object, resolvedService);
            Assert.Equal("{\"To\":\"85261234567\"}", resolvedContext.InputParameters["payload"]);
        }

        [Fact]
        public void OrganizationService_Retrieve_CanBeMocked()
        {
            // 演示如何为 Retrieve 打桩，返回一个内存中的配置实体
            var orgService = new Mock<IOrganizationService>();
            var accountId = Guid.NewGuid();
            var fakeAccount = new Entity("xgate_xgatesmschannelinstanceaccount", accountId);
            fakeAccount["xgate_accountid"] = "test-app-id";
            fakeAccount["xgate_apibaseurl"] = "https://sms-api.xgate.com/sms/2.0";

            orgService
                .Setup(s => s.Retrieve(
                    "xgate_xgatesmschannelinstanceaccount",
                    accountId,
                    It.IsAny<ColumnSet>()))
                .Returns(fakeAccount);

            var result = orgService.Object.Retrieve(
                "xgate_xgatesmschannelinstanceaccount", accountId, new ColumnSet(true));

            Assert.Equal("test-app-id", result.GetAttributeValue<string>("xgate_accountid"));
            orgService.Verify(
                s => s.Retrieve("xgate_xgatesmschannelinstanceaccount", accountId, It.IsAny<ColumnSet>()),
                Times.Once);
        }
    }
}
