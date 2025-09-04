using StackExchange.Redis;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.SlidingWindow
{
    /// <summary>
    /// 滑动窗口工厂接口
    /// </summary>
    public interface ISlidingWindowFactory
    {
        /// <summary>
        /// 创建滑动窗口
        /// </summary>
        /// <param name="key">窗口的唯一标识</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <returns>滑动窗口实例</returns>
        ISlidingWindow CreateSlidingWindow(string key, int maxRequests, TimeSpan windowSize);
    }

    /// <summary>
    /// 内存滑动窗口工厂
    /// </summary>
    public class InMemorySlidingWindowFactory : ISlidingWindowFactory
    {
        public ISlidingWindow CreateSlidingWindow(string key, int maxRequests, TimeSpan windowSize)
        {
            return new InMemorySlidingWindow(maxRequests, windowSize);
        }
    }

    /// <summary>
    /// Redis滑动窗口工厂
    /// </summary>
    public class RedisSlidingWindowFactory : ISlidingWindowFactory
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisSlidingWindow> _logger;
        private readonly string _keyPrefix;

        public RedisSlidingWindowFactory(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisSlidingWindow> logger,
            string keyPrefix = "sliding_window:")
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _keyPrefix = keyPrefix;
        }

        public ISlidingWindow CreateSlidingWindow(string key, int maxRequests, TimeSpan windowSize)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisKey = $"{_keyPrefix}{key}";
            return new RedisSlidingWindow(database, redisKey, maxRequests, windowSize, _logger);
        }
    }
}
