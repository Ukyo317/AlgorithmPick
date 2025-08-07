using StackExchange.Redis;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.TokenBucket
{
    /// <summary>
    /// 令牌桶工厂接口
    /// </summary>
    public interface ITokenBucketFactory
    {
        /// <summary>
        /// 创建令牌桶
        /// </summary>
        /// <param name="key">桶的唯一标识</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="refillRate">令牌补充速率</param>
        /// <returns>令牌桶实例</returns>
        ITokenBucket CreateTokenBucket(string key, int capacity, double refillRate);
    }

    /// <summary>
    /// 内存令牌桶工厂
    /// </summary>
    public class InMemoryTokenBucketFactory : ITokenBucketFactory
    {
        public ITokenBucket CreateTokenBucket(string key, int capacity, double refillRate)
        {
            return new InMemoryTokenBucket(capacity, refillRate);
        }
    }

    /// <summary>
    /// Redis令牌桶工厂
    /// </summary>
    public class RedisTokenBucketFactory : ITokenBucketFactory
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisTokenBucket> _logger;
        private readonly string _keyPrefix;

        public RedisTokenBucketFactory(
            IConnectionMultiplexer connectionMultiplexer, 
            ILogger<RedisTokenBucket> logger,
            string keyPrefix = "rate_limit:")
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _keyPrefix = keyPrefix;
        }

        public ITokenBucket CreateTokenBucket(string key, int capacity, double refillRate)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisKey = $"{_keyPrefix}{key}";
            return new RedisTokenBucket(database, redisKey, capacity, refillRate, _logger);
        }
    }
}
