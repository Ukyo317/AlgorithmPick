using System.Collections.Concurrent;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.SlidingWindow
{
    /// <summary>
    /// 滑动窗口算法实现
    /// 在滑动时间窗口内限制请求数量
    /// </summary>
    public class SlidingWindowAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, ISlidingWindow> _windows = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly ISlidingWindowFactory _windowFactory;
        private readonly ILogger<SlidingWindowAlgorithm> _logger;

        public SlidingWindowAlgorithm(
            int maxRequests,
            TimeSpan windowSize,
            ISlidingWindowFactory windowFactory,
            ILogger<SlidingWindowAlgorithm> logger)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _windowFactory = windowFactory;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var window = _windows.GetOrAdd(key, k => _windowFactory.CreateSlidingWindow(k, _maxRequests, _windowSize));
            var result = await window.TryAcquireAsync(cost);

            var remaining = await window.GetRemainingRequestsAsync();
            var (count, oldestRequest) = await window.GetWindowStatsAsync();
            
            _logger.LogDebug("SlidingWindow - Key: {Key}, Cost: {Cost}, Success: {Success}, Remaining: {Remaining}, WindowCount: {WindowCount}, OldestRequest: {OldestRequest}",
                key, cost, result, remaining, count, oldestRequest);

            return result;
        }

        public async Task<long> GetRemainingTokensAsync(string key)
        {
            if (_windows.TryGetValue(key, out var window))
            {
                return await window.GetRemainingRequestsAsync();
            }
            return _maxRequests;
        }

        public async Task ResetAsync(string key)
        {
            _windows.TryRemove(key, out _);
            await Task.CompletedTask;
        }
    }
}
