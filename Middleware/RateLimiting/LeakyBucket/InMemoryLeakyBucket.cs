namespace AlgorithmPick.Middleware.RateLimiting.LeakyBucket
{
    /// <summary>
    /// 本地内存漏桶数据结构
    /// </summary>
    public class InMemoryLeakyBucket : ILeakyBucket
    {
        private readonly object _lock = new();
        private readonly int _capacity;
        private readonly double _leakRate;
        private double _volume;
        private DateTime _lastLeak;

        public InMemoryLeakyBucket(int capacity, double leakRate)
        {
            _capacity = capacity;
            _leakRate = leakRate;
            _volume = 0;
            _lastLeak = DateTime.UtcNow;
        }

        public Task<double> GetCurrentVolumeAsync()
        {
            lock (_lock)
            {
                Leak();
                return Task.FromResult(_volume);
            }
        }

        public Task<bool> TryAddAsync(int amount)
        {
            lock (_lock)
            {
                Leak();
                
                if (_volume + amount <= _capacity)
                {
                    _volume += amount;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
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
