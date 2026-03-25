namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class TokenRequest
    {
        [DataMember(Name = "appId")]
        public string AppId { get; set; }

        [DataMember(Name = "appSecret")]
        public string AppSecret { get; set; }
    }
}
