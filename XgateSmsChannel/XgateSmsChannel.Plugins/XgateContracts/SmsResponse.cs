namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsResponse
    {
        [DataMember(Name = "CountOfStatus")]
        public SmsCountOfStatus CountOfStatus { get; set; }

        [DataMember(Name = "ReceiveInfo")]
        public List<SmsReceiveInfo> ReceiveInfo { get; set; }
    }
}
