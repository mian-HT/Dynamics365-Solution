namespace XgateWhatsAppChannel.Plugins
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class Response
    {
        [DataMember]
        public Guid ChannelDefinitionId { get; set; }

        [DataMember]
        public string RequestId { get; set; }

        [DataMember]
        public string MessageId { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public ResponseDetails Details { get; set; }
    }
}
