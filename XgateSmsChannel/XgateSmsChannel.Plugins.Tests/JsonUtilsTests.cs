namespace XgateSmsChannel.Plugins.Tests
{
    using System.Collections.Generic;
    using Xunit;
    using XgateSmsChannel.Plugins;
    using XgateSmsChannel.Plugins.XgateContracts;

    public class JsonUtilsTests
    {
        [Fact]
        public void Serialize_SmsRecipient_UsesDataMemberNames()
        {
            var recipient = new SmsRecipient { To = "85261234567", ExternalId = "abc.123" };

            var json = JsonUtils.Serialize(recipient);

            Assert.Contains("\"To\":\"85261234567\"", json);
            Assert.Contains("\"ExternalId\":\"abc.123\"", json);
        }

        [Fact]
        public void Deserialize_SmsRequest_MapsFieldsCorrectly()
        {
            const string json =
                "{\"MessageBody\":\"Hello Xgate\",\"ToList\":[{\"To\":\"85261234567\",\"ExternalId\":\"ext-1\"}]}";

            var request = JsonUtils.Deserialize<SmsRequest>(json);

            Assert.NotNull(request);
            Assert.Equal("Hello Xgate", request.MessageBody);
            Assert.Single(request.ToList);
            Assert.Equal("85261234567", request.ToList[0].To);
            Assert.Equal("ext-1", request.ToList[0].ExternalId);
        }

        [Fact]
        public void SerializeThenDeserialize_SmsRequest_RoundTripsValues()
        {
            var original = new SmsRequest
            {
                MessageBody = "round-trip",
                ToList = new List<SmsRecipient>
                {
                    new SmsRecipient { To = "85260000000", ExternalId = "rt-1" }
                }
            };

            var json = JsonUtils.Serialize(original);
            var restored = JsonUtils.Deserialize<SmsRequest>(json);

            Assert.Equal(original.MessageBody, restored.MessageBody);
            Assert.Equal(original.ToList[0].To, restored.ToList[0].To);
            Assert.Equal(original.ToList[0].ExternalId, restored.ToList[0].ExternalId);
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
