namespace XgateSmsChannel.Plugins
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ProviderDeliveryReport
    {
        [DataMember]
        public string MessageSid { get; set; }

        [DataMember]
        public string From { get; set; }

        [DataMember]
        public string MessageStatus { get; set; }

        [DataMember]
        public string RequestId { get; set; }
    }
}
