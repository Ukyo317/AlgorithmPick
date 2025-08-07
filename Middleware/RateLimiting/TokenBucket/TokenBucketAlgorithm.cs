using System.Collections.Concurrent;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.TokenBucket
{
    /// <summary>
    /// 令牌桶算法实现
    /// 允许突发流量，但长期平均速率受限
    /// </summary>
    public class TokenBucketAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, ITokenBucket> _buckets = new();
        private readonly int _capacity;
        private readonly double _refillRate;
        private readonly ITokenBucketFactory _tokenBucketFactory;
        private readonly ILogger<TokenBucketAlgorithm> _logger;

        public TokenBucketAlgorithm(
            int capacity, 
            int requestsPerSecond, 
            ITokenBucketFactory tokenBucketFactory,
            ILogger<TokenBucketAlgorithm> logger)
        {
            _capacity = capacity;
            _refillRate = requestsPerSecond; // 每秒补充的令牌数
            _tokenBucketFactory = tokenBucketFactory;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var bucket = _buckets.GetOrAdd(key, k => _tokenBucketFactory.CreateTokenBucket(k, _capacity, _refillRate));
            var result = await bucket.TryConsumeAsync(cost);
            
            var remainingTokens = await bucket.GetCurrentTokensAsync();
            _logger.LogDebug("TokenBucket - Key: {Key}, Cost: {Cost}, Success: {Success}, Remaining: {Remaining}", 
                key, cost, result, remainingTokens);
            
            return result;
        }

        public async Task<long> GetRemainingTokensAsync(string key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                var tokens = await bucket.GetCurrentTokensAsync();
                return (long)tokens;
            }
            
            return _capacity;
        }

        public async Task ResetAsync(string key)
        {
            _buckets.TryRemove(key, out _);
            await Task.CompletedTask;
        }
    }
}
