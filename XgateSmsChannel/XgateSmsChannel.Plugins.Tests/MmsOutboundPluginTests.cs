namespace XgateSmsChannel.Plugins.Tests
{
    using System;
    using Xunit;
    using XgateSmsChannel.Plugins;
    using XgateSmsChannel.Plugins.ChannelContracts;

    public class MmsOutboundPluginTests
    {
        [Fact]
        public void Execute_WhenProviderIsNotConfigured_ReturnsStructuredFailure()
        {
            var pipeline = new PluginPipeline();
            pipeline.SetPayload(JsonUtils.Serialize(new Payload
            {
                ChannelDefinitionId = Guid.NewGuid(),
                RequestId = "mms-request-1",
                From = "MMS-SENDER",
                To = "85261234567"
            }));

            new MmsOutboundPlugin().Execute(pipeline.ServiceProvider.Object);

            var response = JsonUtils.Deserialize<Response>(pipeline.GetResponseString());
            Assert.Equal("Failed", response.Status);
            Assert.Equal("mms-request-1", response.RequestId);
            Assert.Equal("mms-request-1", response.MessageId);
            Assert.Equal(
                MmsOutboundPlugin.ProviderNotConfiguredMessage,
                response.StatusDetails["ErrorDetails"]);
        }
    }
}
