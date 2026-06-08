namespace XgateSmsChannel.Plugins.Tests
{
    using Microsoft.Xrm.Sdk;
    using Moq;
    using Xunit;
    using XgateSmsChannel.Plugins;

    public class InboundPluginTests
    {
        [Fact]
        public void Execute_ProxiesPayloadToInboundApi_AndReturnsResponseMessage()
        {
            var pipeline = new PluginPipeline();
            pipeline.SetPayload("{\"text\":\"incoming sms\"}");
            pipeline.SetupExecuteResponse("inbound-ok");

            new InboundPlugin().Execute(pipeline.ServiceProvider.Object);

            Assert.Equal("inbound-ok", pipeline.GetResponseString());

            // 校验请求名与入参原样透传
            pipeline.OrgService.Verify(
                s => s.Execute(It.Is<OrganizationRequest>(r =>
                    r.RequestName == "msdyn_D365ChannelsInbound" &&
                    (string)r.Parameters["inboundPayLoad"] == "{\"text\":\"incoming sms\"}")),
                Times.Once);
        }
    }
}
