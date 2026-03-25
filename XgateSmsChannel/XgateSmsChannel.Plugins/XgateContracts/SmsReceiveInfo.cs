namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsReceiveInfo
    {
        [DataMember(Name = "MessageId")]
        public string MessageId { get; set; }
    }
}
