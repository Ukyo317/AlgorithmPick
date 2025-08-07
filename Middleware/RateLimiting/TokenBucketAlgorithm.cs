using System.Collections.Concurrent;

namespace AlgorithmPick.Middleware.RateLimiting
{
    /// <summary>
    /// 令牌桶算法实现
    /// 允许突发流量，但长期平均速率受限
    /// </summary>
    public class TokenBucketAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
        private readonly int _capacity;
        private readonly double _refillRate;
        private readonly ILogger<TokenBucketAlgorithm> _logger;

        public TokenBucketAlgorithm(int capacity, int requestsPerSecond, ILogger<TokenBucketAlgorithm> logger)
        {
            _capacity = capacity;
            _refillRate = requestsPerSecond; // 每秒补充的令牌数
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(_capacity, _refillRate));
            var result = bucket.TryConsume(cost);
            
            _logger.LogDebug("TokenBucket - Key: {Key}, Cost: {Cost}, Success: {Success}, Remaining: {Remaining}", 
                key, cost, result, bucket.CurrentTokens);
            
            return await Task.FromResult(result);
        }

        public async Task<long> GetRemainingTokensAsync(string key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                return await Task.FromResult((long)bucket.CurrentTokens);
            }
            return await Task.FromResult((long)_capacity);
        }

        public async Task ResetAsync(string key)
        {
            _buckets.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        private class TokenBucket
        {
            private readonly object _lock = new();
            private readonly int _capacity;
            private readonly double _refillRate;
            private double _tokens;
            private DateTime _lastRefill;

            public TokenBucket(int capacity, double refillRate)
            {
                _capacity = capacity;
                _refillRate = refillRate;
                _tokens = capacity; // 初始化时桶是满的
                _lastRefill = DateTime.UtcNow;
            }

            public double CurrentTokens
            {
                get
                {
                    lock (_lock)
                    {
                        Refill();
                        return _tokens;
                    }
                }
            }

            public bool TryConsume(int cost)
            {
                lock (_lock)
                {
                    Refill();
                    
                    if (_tokens >= cost)
                    {
                        _tokens -= cost;
                        return true;
                    }
                    return false;
                }
            }

            private void Refill()
            {
                var now = DateTime.UtcNow;
                var timePassed = (now - _lastRefill).TotalSeconds;
                
                if (timePassed > 0)
                {
                    var tokensToAdd = timePassed * _refillRate;
                    _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                    _lastRefill = now;
                }
            }
        }
    }
}
