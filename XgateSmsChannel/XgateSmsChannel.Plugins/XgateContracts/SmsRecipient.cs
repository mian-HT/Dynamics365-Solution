namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsRecipient
    {
        [DataMember(Name = "To")]
        public string To { get; set; }

        [DataMember(Name = "ExternalId")]
        public string ExternalId { get; set; }
    }
}
