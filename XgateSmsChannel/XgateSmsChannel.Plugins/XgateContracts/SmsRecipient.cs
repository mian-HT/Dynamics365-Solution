namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Runtime.Serialization;

    // 单个收件人的受理明细
    [DataContract]
    public class SmsRecipient
    {
        // 收件人号码（E.164），例如 +85291234567
        [DataMember(Name = "to")]
        public string To { get; set; }

        // 渠道返回的消息唯一 id，后续状态回调以此匹配；渠道未返回时为空
        [DataMember(Name = "messageId")]
        public string MessageId { get; set; }

        // 受理阶段状态：同步返回时固定为 Sending，终态经 webhook 回调另行推送
        [DataMember(Name = "status")]
        public string Status { get; set; }
    }
}
