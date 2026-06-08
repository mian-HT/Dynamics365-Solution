namespace XgateSmsChannel.Plugins
{
    using System;

    /// <summary>
    /// Xgate 短信网关相关的业务异常（配置缺失、登录失败、发送被拒等）。
    /// 用专用异常类型替代直接抛 System.Exception，便于按类型捕获与排查。
    /// </summary>
    [Serializable]
    public class XgateGatewayException : Exception
    {
        public XgateGatewayException()
        {
        }

        public XgateGatewayException(string message)
            : base(message)
        {
        }

        public XgateGatewayException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XgateGatewayException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
