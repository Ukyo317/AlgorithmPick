namespace AlgorithmPick.Middleware.RateLimiting
{
    /// <summary>
    /// 限流配置选项
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// 限流算法类型
        /// </summary>
        public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.TokenBucket;

        /// <summary>
        /// 每秒允许的请求数量
        /// </summary>
        public int RequestsPerSecond { get; set; } = 10;

        /// <summary>
        /// 桶容量（对于令牌桶算法）或队列容量（对于漏桶算法）
        /// </summary>
        public int Capacity { get; set; } = 20;

        /// <summary>
        /// 获取客户端标识的函数
        /// </summary>
        public Func<HttpContext, string> KeyGenerator { get; set; } = context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        /// <summary>
        /// 限流触发时的响应状态码
        /// </summary>
        public int StatusCode { get; set; } = 429;

        /// <summary>
        /// 限流触发时的响应消息
        /// </summary>
        public string Message { get; set; } = "Too Many Requests";

        /// <summary>
        /// 是否在响应头中包含限流信息
        /// </summary>
        public bool IncludeHeaders { get; set; } = true;
    }

    /// <summary>
    /// 限流算法类型
    /// </summary>
    public enum RateLimitAlgorithm
    {
        /// <summary>
        /// 令牌桶算法
        /// </summary>
        TokenBucket,
        /// <summary>
        /// 漏桶算法
        /// </summary>
        LeakyBucket
    }
}
