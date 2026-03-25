namespace XgateWhatsAppChannel.Plugins
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class ResponseDetails
    {
        [DataMember]
        public string Reason { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
