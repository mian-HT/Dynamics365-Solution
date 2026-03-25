namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsRequest
    {
        [DataMember(Name = "MessageBody")]
        public string MessageBody { get; set; }

        [DataMember(Name = "ToList")]
        public List<SmsRecipient> ToList { get; set; }
    }
}
