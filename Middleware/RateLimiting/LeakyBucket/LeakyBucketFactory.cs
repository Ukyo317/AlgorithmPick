using StackExchange.Redis;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.LeakyBucket
{
    /// <summary>
    /// 漏桶工厂接口
    /// </summary>
    public interface ILeakyBucketFactory
    {
        /// <summary>
        /// 创建漏桶
        /// </summary>
        /// <param name="key">桶的唯一标识</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="leakRate">漏出速率</param>
        /// <returns>漏桶实例</returns>
        ILeakyBucket CreateLeakyBucket(string key, int capacity, double leakRate);
    }

    /// <summary>
    /// 内存漏桶工厂
    /// </summary>
    public class InMemoryLeakyBucketFactory : ILeakyBucketFactory
    {
        public ILeakyBucket CreateLeakyBucket(string key, int capacity, double leakRate)
        {
            return new InMemoryLeakyBucket(capacity, leakRate);
        }
    }

    /// <summary>
    /// Redis漏桶工厂
    /// </summary>
    public class RedisLeakyBucketFactory : ILeakyBucketFactory
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisLeakyBucket> _logger;
        private readonly string _keyPrefix;

        public RedisLeakyBucketFactory(
            IConnectionMultiplexer connectionMultiplexer, 
            ILogger<RedisLeakyBucket> logger,
            string keyPrefix = "leaky_bucket:")
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _keyPrefix = keyPrefix;
        }

        public ILeakyBucket CreateLeakyBucket(string key, int capacity, double leakRate)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisKey = $"{_keyPrefix}{key}";
            return new RedisLeakyBucket(database, redisKey, capacity, leakRate, _logger);
        }
    }
}
