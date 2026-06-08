namespace XgateSmsChannel.Plugins.Tests
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Moq;
    using Xunit;
    using XgateSmsChannel.Plugins;

    public class DeliveryReportPluginTests
    {
        [Fact]
        public void Execute_DelivrdState_DecodesExternalIdAndReportsDelivered()
        {
            var msgGuid = Guid.NewGuid();
            var externalId = BuildExternalId(msgGuid, "85261234567");
            var report = RunAndCaptureReport($"externalId={externalId}&state=DELIVRD&messageID=GW-100");

            Assert.Equal("Delivered", report.Status);
            Assert.Equal(msgGuid.ToString(), report.RequestId);
            Assert.Equal("85261234567", report.From);
            Assert.Equal("GW-100", report.MessageId);
        }

        [Theory]
        [InlineData("UNDELIV", "Failed")]
        [InlineData("REJECTED", "Failed")]
        [InlineData("ACCEPTD", "Sent")]
        [InlineData("", "Sent")]
        public void Execute_MapsGatewayStateToChannelStatus(string state, string expectedStatus)
        {
            var externalId = BuildExternalId(Guid.NewGuid(), "10690000");
            var report = RunAndCaptureReport($"externalId={externalId}&state={state}&messageID=GW-1");

            Assert.Equal(expectedStatus, report.Status);
        }

        [Fact]
        public void Execute_ExternalIdWithoutSeparator_KeepsRawValueAndEmptyFrom()
        {
            var report = RunAndCaptureReport("externalId=plainvalue&state=DELIVRD&messageID=GW-2");

            Assert.Equal("plainvalue", report.RequestId);
            Assert.Equal(string.Empty, report.From);
        }

        [Fact]
        public void Execute_InvalidBase64Prefix_KeepsRawValueButParsesFrom()
        {
            // "zz!" 不是合法 Base64，解码失败 -> 保留原始 externalId，但仍解析出发件人号码
            var report = RunAndCaptureReport("externalId=zz!.85260000000&state=DELIVRD&messageID=GW-3");

            Assert.Contains("zz!", report.RequestId);
            Assert.Equal("85260000000", report.From);
        }

        [Fact]
        public void Execute_ReturnsResponseMessageToOutputParameters()
        {
            var pipeline = new PluginPipeline();
            pipeline.SetPayload("externalId=plain&state=DELIVRD&messageID=GW-9");
            pipeline.SetupExecuteResponse("dr-ok");

            new DeliveryReportPlugin().Execute(pipeline.ServiceProvider.Object);

            Assert.Equal("dr-ok", pipeline.GetResponseString());
            pipeline.OrgService.Verify(
                s => s.Execute(It.Is<OrganizationRequest>(r => r.RequestName == "msdyn_D365ChannelsNotification")),
                Times.Once);
        }

        // ----------------- 测试辅助 -----------------

        private static string BuildExternalId(Guid guid, string from)
        {
            var msgB64 = Convert.ToBase64String(guid.ToByteArray())
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            return $"{msgB64}.{from}";
        }

        private static DeliveryReport RunAndCaptureReport(string payload)
        {
            var pipeline = new PluginPipeline();
            pipeline.SetPayload(payload);

            OrganizationRequest captured = null;
            var response = new OrganizationResponse();
            response.Results["responseMessage"] = "ok";
            pipeline.OrgService
                .Setup(s => s.Execute(It.IsAny<OrganizationRequest>()))
                .Callback<OrganizationRequest>(r => captured = r)
                .Returns(response);

            new DeliveryReportPlugin().Execute(pipeline.ServiceProvider.Object);

            Assert.NotNull(captured);
            var notificationPayload = (string)captured.Parameters["notificationPayLoad"];
            return JsonUtils.Deserialize<DeliveryReport>(notificationPayload);
        }
    }
}
