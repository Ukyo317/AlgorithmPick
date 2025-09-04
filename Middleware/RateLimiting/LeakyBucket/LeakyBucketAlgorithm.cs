using System.Collections.Concurrent;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.LeakyBucket
{
    /// <summary>
    /// 漏桶算法实现
    /// 以恒定速率处理请求，平滑突发流量
    /// </summary>
    public class LeakyBucketAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, ILeakyBucket> _buckets = new();
        private readonly int _capacity;
        private readonly double _leakRate;
        private readonly ILeakyBucketFactory _leakyBucketFactory;
        private readonly ILogger<LeakyBucketAlgorithm> _logger;

        public LeakyBucketAlgorithm(
            int capacity, 
            int requestsPerSecond, 
            ILeakyBucketFactory leakyBucketFactory,
            ILogger<LeakyBucketAlgorithm> logger)
        {
            _capacity = capacity;
            _leakRate = requestsPerSecond; // 每秒漏出的请求数
            _leakyBucketFactory = leakyBucketFactory;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var bucket = _buckets.GetOrAdd(key, k => _leakyBucketFactory.CreateLeakyBucket(k, _capacity, _leakRate));
            var result = await bucket.TryAddAsync(cost);
            
            var currentVolume = await bucket.GetCurrentVolumeAsync();
            _logger.LogDebug("LeakyBucket - Key: {Key}, Cost: {Cost}, Success: {Success}, Current: {Current}", 
                key, cost, result, currentVolume);
            
            return result;
        }

        public async Task<long> GetRemainingTokensAsync(string key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                var volume = await bucket.GetCurrentVolumeAsync();
                return (long)(_capacity - volume);
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
