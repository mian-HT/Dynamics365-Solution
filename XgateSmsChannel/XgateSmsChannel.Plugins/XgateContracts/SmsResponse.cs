namespace XgateSmsChannel.Plugins.XgateContracts
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    // 发送接口的同步响应结构。
    // 发送失败时接口返回非 2xx + 错误信息，不会返回本结构。
    [DataContract]
    public class SmsResponse
    {
        // 渠道是否已受理本次请求。true=已提交到短信通道（仅受理，非最终送达）
        [DataMember(Name = "accepted")]
        public bool Accepted { get; set; }

        // 实际使用的通道：dms3 / smsc
        [DataMember(Name = "provider")]
        public string Provider { get; set; }

        // 回显请求里的 requestId，便于对账；请求未带则为空
        [DataMember(Name = "requestId")]
        public string RequestId { get; set; }

        // 各收件人受理明细（当前每次一个收件人，仍用数组以兼容将来批量）
        [DataMember(Name = "recipients")]
        public List<SmsRecipient> Recipients { get; set; }
    }
}
