using System.Collections.Concurrent;
using AlgorithmPick.Middleware.RateLimiting.Core;

namespace AlgorithmPick.Middleware.RateLimiting.FixedWindow
{
    /// <summary>
    /// 固定窗口计数器算法实现
    /// 在固定时间窗口内限制请求数量
    /// </summary>
    public class FixedWindowAlgorithm : IRateLimitingAlgorithm
    {
        private readonly ConcurrentDictionary<string, IFixedWindow> _windows = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly IFixedWindowFactory _windowFactory;
        private readonly ILogger<FixedWindowAlgorithm> _logger;

        public FixedWindowAlgorithm(
            int maxRequests,
            TimeSpan windowSize,
            IFixedWindowFactory windowFactory,
            ILogger<FixedWindowAlgorithm> logger)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _windowFactory = windowFactory;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(string key, int cost = 1)
        {
            var window = _windows.GetOrAdd(key, k => _windowFactory.CreateFixedWindow(k, _maxRequests, _windowSize));
            var result = await window.TryAcquireAsync(cost);

            var remaining = await window.GetRemainingRequestsAsync();
            var nextWindow = await window.GetNextWindowStartAsync();
            
            _logger.LogDebug("FixedWindow - Key: {Key}, Cost: {Cost}, Success: {Success}, Remaining: {Remaining}, NextWindow: {NextWindow}",
                key, cost, result, remaining, DateTimeOffset.FromUnixTimeSeconds(nextWindow));

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
