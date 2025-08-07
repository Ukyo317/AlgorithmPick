namespace AlgorithmPick.Middleware.RateLimiting.TokenBucket
{
    /// <summary>
    /// 本地内存令牌桶数据结构
    /// </summary>
    public class InMemoryTokenBucket : ITokenBucket
    {
        private readonly object _lock = new();
        private readonly int _capacity;
        private readonly double _refillRate;
        private double _tokens;
        private DateTime _lastRefill;

        public InMemoryTokenBucket(int capacity, double refillRate)
        {
            _capacity = capacity;
            _refillRate = refillRate;
            _tokens = capacity; // 初始化时桶是满的
            _lastRefill = DateTime.UtcNow;
        }

        public Task<double> GetCurrentTokensAsync()
        {
            lock (_lock)
            {
                Refill();
                return Task.FromResult(_tokens);
            }
        }

        public Task<bool> TryConsumeAsync(int cost)
        {
            lock (_lock)
            {
                Refill();
                
                if (_tokens >= cost)
                {
                    _tokens -= cost;
                    return Task.FromResult(true);
                }
                
                return Task.FromResult(false);
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
