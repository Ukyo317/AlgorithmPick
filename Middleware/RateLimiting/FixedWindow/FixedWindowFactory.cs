using StackExchange.Redis;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.FixedWindow
{
    /// <summary>
    /// 固定窗口工厂接口
    /// </summary>
    public interface IFixedWindowFactory
    {
        /// <summary>
        /// 创建固定窗口计数器
        /// </summary>
        /// <param name="key">窗口的唯一标识</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <returns>固定窗口实例</returns>
        IFixedWindow CreateFixedWindow(string key, int maxRequests, TimeSpan windowSize);
    }

    /// <summary>
    /// 内存固定窗口工厂
    /// </summary>
    public class InMemoryFixedWindowFactory : IFixedWindowFactory
    {
        public IFixedWindow CreateFixedWindow(string key, int maxRequests, TimeSpan windowSize)
        {
            return new InMemoryFixedWindow(maxRequests, windowSize);
        }
    }

    /// <summary>
    /// Redis固定窗口工厂
    /// </summary>
    public class RedisFixedWindowFactory : IFixedWindowFactory
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisFixedWindow> _logger;
        private readonly string _keyPrefix;

        public RedisFixedWindowFactory(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisFixedWindow> logger,
            string keyPrefix = "fixed_window:")
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _keyPrefix = keyPrefix;
        }

        public IFixedWindow CreateFixedWindow(string key, int maxRequests, TimeSpan windowSize)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisKey = $"{_keyPrefix}{key}";
            return new RedisFixedWindow(database, redisKey, maxRequests, windowSize, _logger);
        }
    }
}
