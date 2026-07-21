namespace XgateWhatsAppChannel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    [DataContract]
    public class Payload
    {
        [DataMember]
        public Guid ChannelDefinitionId { get; set; }

        [DataMember]
        public string RequestId { get; set; }

        [DataMember]
        public string From { get; set; }

        [DataMember]
        public string To { get; set; }

        [DataMember]
        public IDictionary<string, string> Message { get; set; } = new Dictionary<string, string>();

        // 旅程运行时的上下文。仅正式旅程发送时有值；Test Send 时为 null / 字段为空。
        // 个性化替换依赖其中的收件人实体 ID（UserId）与类型（UserEntityType）。
        [DataMember]
        public MarketingAppContext MarketingAppContext { get; set; }
    }

    [DataContract]
    public class MarketingAppContext
    {
        [DataMember]
        public string CustomerJourneyId { get; set; }

        // 接收此消息的对象（contact/lead 记录）的 ID；Test Send 时为空。
        // 类型用 string 承接，避免 GUID 反序列化差异，使用时再 Guid.TryParse。
        [DataMember]
        public string UserId { get; set; }

        // 接收对象的实体逻辑名（contact / lead）。
        [DataMember]
        public string UserEntityType { get; set; }

        [DataMember]
        public bool IsTestSend { get; set; }
    }
}
