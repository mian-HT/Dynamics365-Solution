namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    [DataContract]
    public class TokenResponse
    {
        [DataMember(Name = "accessToken")]
        public string AccessToken { get; set; }
    }
}
