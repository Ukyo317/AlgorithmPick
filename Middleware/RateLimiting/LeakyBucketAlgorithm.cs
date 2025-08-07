using System.Collections.Concurrent;

namespace AlgorithmPick.Middleware.RateLimiting
{
    /// <summary>
    /// 漏桶算法实现
    /// 以恒定速率处理请求，平滑突发流量
    /// </summary>
    public class LeakyBucketAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, LeakyBucket> _buckets = new();
        private readonly int _capacity;
        private readonly double _leakRate;
        private readonly ILogger<LeakyBucketAlgorithm> _logger;

        public LeakyBucketAlgorithm(int capacity, int requestsPerSecond, ILogger<LeakyBucketAlgorithm> logger)
        {
            _capacity = capacity;
            _leakRate = requestsPerSecond; // 每秒漏出的请求数
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var bucket = _buckets.GetOrAdd(key, _ => new LeakyBucket(_capacity, _leakRate));
            var result = bucket.TryAdd(cost);
            
            _logger.LogDebug("LeakyBucket - Key: {Key}, Cost: {Cost}, Success: {Success}, Current: {Current}", 
                key, cost, result, bucket.CurrentVolume);
            
            return await Task.FromResult(result);
        }

        public async Task<long> GetRemainingTokensAsync(string key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                return await Task.FromResult((long)(_capacity - bucket.CurrentVolume));
            }
            return await Task.FromResult((long)_capacity);
        }

        public async Task ResetAsync(string key)
        {
            _buckets.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        private class LeakyBucket
        {
            private readonly object _lock = new();
            private readonly int _capacity;
            private readonly double _leakRate;
            private double _volume;
            private DateTime _lastLeak;

            public LeakyBucket(int capacity, double leakRate)
            {
                _capacity = capacity;
                _leakRate = leakRate;
                _volume = 0;
                _lastLeak = DateTime.UtcNow;
            }

            public double CurrentVolume
            {
                get
                {
                    lock (_lock)
                    {
                        Leak();
                        return _volume;
                    }
                }
            }

            public bool TryAdd(int amount)
            {
                lock (_lock)
                {
                    Leak();
                    
                    if (_volume + amount <= _capacity)
                    {
                        _volume += amount;
                        return true;
                    }
                    return false;
                }
            }

            private void Leak()
            {
                var now = DateTime.UtcNow;
                var timePassed = (now - _lastLeak).TotalSeconds;
                
                if (timePassed > 0)
                {
                    var volumeToLeak = timePassed * _leakRate;
                    _volume = Math.Max(0, _volume - volumeToLeak);
                    _lastLeak = now;
                }
            }
        }
    }
}
