using StackExchange.Redis;

namespace AlgorithmPick.Middleware.RateLimiting.FixedWindow
{
    /// <summary>
    /// 基于Redis的分布式固定窗口计数器实现
    /// </summary>
    public class RedisFixedWindow : IFixedWindow
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly ILogger<RedisFixedWindow> _logger;

        public RedisFixedWindow(
            IDatabase database,
            string key,
            int maxRequests,
            TimeSpan windowSize,
            ILogger<RedisFixedWindow> logger)
        {
            _database = database;
            _key = key;
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(int cost = 1)
        {
            var windowKey = GetCurrentWindowKey();
            
            // 使用Lua脚本确保操作的原子性
            const string script = @"
                local key = KEYS[1]
                local max_requests = tonumber(ARGV[1])
                local cost = tonumber(ARGV[2])
                local window_size_seconds = tonumber(ARGV[3])
                
                local current_count = tonumber(redis.call('GET', key) or 0)
                
                if current_count + cost <= max_requests then
                    local new_count = redis.call('INCRBY', key, cost)
                    if new_count == cost then
                        -- 这是窗口内的第一个请求，设置过期时间
                        redis.call('EXPIRE', key, window_size_seconds)
                    end
                    return 1
                else
                    return 0
                end
            ";

            try
            {
                var result = await _database.ScriptEvaluateAsync(script,
                    new RedisKey[] { windowKey },
                    new RedisValue[] { _maxRequests, cost, (int)_windowSize.TotalSeconds });

                return (int)result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire permit for key {Key}", _key);
                throw;
            }
        }

        public async Task<long> GetRemainingRequestsAsync()
        {
            var windowKey = GetCurrentWindowKey();
            var currentCount = await _database.StringGetAsync(windowKey);
            var used = currentCount.HasValue ? (int)currentCount : 0;
            return Math.Max(0, _maxRequests - used);
        }

        public async Task<long> GetNextWindowStartAsync()
        {
            var currentWindow = GetCurrentWindowStart();
            var nextWindow = currentWindow + (long)_windowSize.TotalSeconds;
            return await Task.FromResult(nextWindow);
        }

        private string GetCurrentWindowKey()
        {
            var windowStart = GetCurrentWindowStart();
            return $"{_key}:window:{windowStart}";
        }

        private long GetCurrentWindowStart()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowSizeSeconds = (long)_windowSize.TotalSeconds;
            return (now / windowSizeSeconds) * windowSizeSeconds;
        }
    }
}
