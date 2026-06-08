namespace XgateSmsChannel.Plugins.Tests
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Moq;

    /// <summary>
    /// 可复用的 D365 插件执行管线 mock（serviceProvider -> tracing/context/factory/orgService）。
    /// </summary>
    internal sealed class PluginPipeline
    {
        public Mock<IServiceProvider> ServiceProvider { get; }
        public Mock<IPluginExecutionContext> PluginContext { get; }
        public Mock<IOrganizationService> OrgService { get; }
        public ParameterCollection InputParameters { get; }
        public ParameterCollection OutputParameters { get; }

        public PluginPipeline(Guid? organizationId = null)
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
            PluginContext.Setup(c => c.OrganizationId).Returns(organizationId ?? Guid.NewGuid());

            factory.Setup(f => f.CreateOrganizationService(It.IsAny<Guid?>())).Returns(OrgService.Object);

            ServiceProvider.Setup(p => p.GetService(typeof(ITracingService))).Returns(tracing.Object);
            ServiceProvider.Setup(p => p.GetService(typeof(IPluginExecutionContext))).Returns(PluginContext.Object);
            ServiceProvider.Setup(p => p.GetService(typeof(IOrganizationServiceFactory))).Returns(factory.Object);
        }

        public void SetPayload(string payload)
        {
            InputParameters["payload"] = payload;
        }

        public string GetResponseString()
        {
            return (string)OutputParameters["response"];
        }

        // 让 orgService.Execute 返回带有 responseMessage 结果的 OrganizationResponse
        public OrganizationResponse SetupExecuteResponse(string responseMessage)
        {
            var response = new OrganizationResponse();
            response.Results["responseMessage"] = responseMessage;
            OrgService.Setup(s => s.Execute(It.IsAny<OrganizationRequest>())).Returns(response);
            return response;
        }
    }
}
