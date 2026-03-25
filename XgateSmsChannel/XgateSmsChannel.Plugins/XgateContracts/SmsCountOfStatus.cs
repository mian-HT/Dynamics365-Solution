namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SmsCountOfStatus
    {
        [DataMember(Name = "SUCCESS")]
        public int Success { get; set; }

        [DataMember(Name = "FAILED")]
        public int Failed { get; set; }
    }
}
