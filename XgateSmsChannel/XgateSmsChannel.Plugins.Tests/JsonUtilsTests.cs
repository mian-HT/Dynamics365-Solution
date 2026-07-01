namespace XgateSmsChannel.Plugins.Tests
{
    using Xunit;
    using XgateSmsChannel.Plugins;
    using XgateSmsChannel.Plugins.XgateContracts;

    public class JsonUtilsTests
    {
        [Fact]
        public void Serialize_SmsRequest_UsesDataMemberNames()
        {
            var request = new SmsRequest
            {
                OrganizationId = "org-001",
                RequestId = "req-001",
                From = "10690000",
                To = "85261234567",
                Message = "Hello Xgate"
            };

            var json = JsonUtils.Serialize(request);

            Assert.Contains("\"organizationId\":\"org-001\"", json);
            Assert.Contains("\"requestId\":\"req-001\"", json);
            Assert.Contains("\"from\":\"10690000\"", json);
            Assert.Contains("\"to\":\"85261234567\"", json);
            Assert.Contains("\"message\":\"Hello Xgate\"", json);
        }

        [Fact]
        public void Deserialize_SmsRequest_MapsFieldsCorrectly()
        {
            const string json =
                "{\"organizationId\":\"org-001\",\"requestId\":\"req-001\",\"from\":\"10690000\",\"to\":\"85261234567\",\"message\":\"Hello Xgate\"}";

            var request = JsonUtils.Deserialize<SmsRequest>(json);

            Assert.NotNull(request);
            Assert.Equal("org-001", request.OrganizationId);
            Assert.Equal("req-001", request.RequestId);
            Assert.Equal("10690000", request.From);
            Assert.Equal("85261234567", request.To);
            Assert.Equal("Hello Xgate", request.Message);
        }

        [Fact]
        public void SerializeThenDeserialize_SmsRequest_RoundTripsValues()
        {
            var original = new SmsRequest
            {
                OrganizationId = "org-rt",
                RequestId = "req-rt",
                From = "10690000",
                To = "85260000000",
                Message = "round-trip"
            };

            var json = JsonUtils.Serialize(original);
            var restored = JsonUtils.Deserialize<SmsRequest>(json);

            Assert.Equal(original.OrganizationId, restored.OrganizationId);
            Assert.Equal(original.RequestId, restored.RequestId);
            Assert.Equal(original.From, restored.From);
            Assert.Equal(original.To, restored.To);
            Assert.Equal(original.Message, restored.Message);
        }

        [Fact]
        public void Deserialize_StringType_ReturnsRawInput()
        {
            const string raw = "just-a-plain-string";

            var result = JsonUtils.Deserialize<string>(raw);

            Assert.Equal(raw, result);
        }
    }
}
