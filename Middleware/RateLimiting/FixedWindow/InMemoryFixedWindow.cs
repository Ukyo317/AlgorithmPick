namespace AlgorithmPick.Middleware.RateLimiting.FixedWindow
{
    /// <summary>
    /// 内存固定窗口计数器实现
    /// </summary>
    public class InMemoryFixedWindow : IFixedWindow
    {
        private readonly object _lock = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private int _currentCount;
        private DateTime _windowStart;

        public InMemoryFixedWindow(int maxRequests, TimeSpan windowSize)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _currentCount = 0;
            _windowStart = DateTime.UtcNow;
        }

        public Task<bool> TryAcquireAsync(int cost = 1)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // 检查是否需要重置窗口
                if (now >= _windowStart.Add(_windowSize))
                {
                    ResetWindow(now);
                }

                // 检查是否超过限制
                if (_currentCount + cost <= _maxRequests)
                {
                    _currentCount += cost;
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        public Task<long> GetRemainingRequestsAsync()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // 检查是否需要重置窗口
                if (now >= _windowStart.Add(_windowSize))
                {
                    ResetWindow(now);
                }

                return Task.FromResult((long)Math.Max(0, _maxRequests - _currentCount));
            }
        }

        public Task<long> GetNextWindowStartAsync()
        {
            lock (_lock)
            {
                var nextWindowStart = _windowStart.Add(_windowSize);
                return Task.FromResult(((DateTimeOffset)nextWindowStart).ToUnixTimeSeconds());
            }
        }

        private void ResetWindow(DateTime now)
        {
            // 计算应该重置到哪个窗口
            var elapsedWindows = (int)((now - _windowStart).Ticks / _windowSize.Ticks);
            _windowStart = _windowStart.Add(TimeSpan.FromTicks(_windowSize.Ticks * elapsedWindows));
            _currentCount = 0;
        }
    }
}
