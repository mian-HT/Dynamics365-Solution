namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsRequest
    {
        [DataMember(Name = "organizationId")]
        public string OrganizationId { get; set; }

        [DataMember(Name = "requestId")]
        public string RequestId { get; set; }

        [DataMember(Name = "from")]
        public string From { get; set; }

        [DataMember(Name = "to")]
        public string To { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
