namespace AlgorithmPick.Middleware.RateLimiting.SlidingWindow
{
    /// <summary>
    /// 内存滑动窗口实现
    /// 使用队列记录请求时间戳
    /// </summary>
    public class InMemorySlidingWindow : ISlidingWindow
    {
        private readonly object _lock = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly Queue<DateTime> _requestTimes = new();

        public InMemorySlidingWindow(int maxRequests, TimeSpan windowSize)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
        }

        public Task<bool> TryAcquireAsync(int cost = 1)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                CleanupExpiredRequests(now);

                if (_requestTimes.Count + cost <= _maxRequests)
                {
                    // 添加请求时间戳
                    for (int i = 0; i < cost; i++)
                    {
                        _requestTimes.Enqueue(now);
                    }
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public Task<long> GetRemainingRequestsAsync()
        {
            lock (_lock)
            {
                CleanupExpiredRequests(DateTime.UtcNow);
                return Task.FromResult((long)Math.Max(0, _maxRequests - _requestTimes.Count));
            }
        }

        public Task<(int count, DateTime oldestRequest)> GetWindowStatsAsync()
        {
            lock (_lock)
            {
                CleanupExpiredRequests(DateTime.UtcNow);
                var count = _requestTimes.Count;
                var oldestRequest = count > 0 ? _requestTimes.Peek() : DateTime.MinValue;
                return Task.FromResult((count, oldestRequest));
            }
        }

        private void CleanupExpiredRequests(DateTime now)
        {
            var cutoff = now - _windowSize;
            while (_requestTimes.Count > 0 && _requestTimes.Peek() <= cutoff)
            {
                _requestTimes.Dequeue();
            }
        }
    }
}
