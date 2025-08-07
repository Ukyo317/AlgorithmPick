using StackExchange.Redis;

namespace AlgorithmPick.Middleware.RateLimiting.SlidingWindow
{
    /// <summary>
    /// 基于Redis的分布式滑动窗口实现
    /// 使用Redis的Sorted Set存储请求时间戳
    /// </summary>
    public class RedisSlidingWindow : ISlidingWindow
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly ILogger<RedisSlidingWindow> _logger;

        public RedisSlidingWindow(
            IDatabase database,
            string key,
            int maxRequests,
            TimeSpan windowSize,
            ILogger<RedisSlidingWindow> logger)
        {
            _database = database;
            _key = key;
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _logger = logger;
        }

        public async Task<bool> TryAcquireAsync(int cost = 1)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)_windowSize.TotalMilliseconds;

            // 使用Lua脚本确保操作的原子性
            const string script = @"
                local key = KEYS[1]
                local now = tonumber(ARGV[1])
                local window_start = tonumber(ARGV[2])
                local max_requests = tonumber(ARGV[3])
                local cost = tonumber(ARGV[4])
                local window_size_ms = tonumber(ARGV[5])
                
                -- 清理过期的请求记录
                redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)
                
                -- 获取当前窗口内的请求数
                local current_count = redis.call('ZCARD', key)
                
                if current_count + cost <= max_requests then
                    -- 添加新的请求记录
                    for i = 1, cost do
                        -- 使用时间戳加随机数作为score，确保唯一性
                        local score = now + (i - 1) * 0.001
                        redis.call('ZADD', key, score, score)
                    end
                    
                    -- 设置过期时间（窗口大小的2倍，确保数据及时清理）
                    redis.call('EXPIRE', key, math.ceil(window_size_ms / 1000) * 2)
                    
                    return 1
                else
                    return 0
                end
            ";

            try
            {
                var result = await _database.ScriptEvaluateAsync(script,
                    new RedisKey[] { _key },
                    new RedisValue[] { now, windowStart, _maxRequests, cost, (long)_windowSize.TotalMilliseconds });

                return (int)result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire permit for sliding window key {Key}", _key);
                throw;
            }
        }

        public async Task<long> GetRemainingRequestsAsync()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)_windowSize.TotalMilliseconds;

            // 清理过期记录并获取当前计数
            const string script = @"
                local key = KEYS[1]
                local window_start = tonumber(ARGV[1])
                
                redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)
                return redis.call('ZCARD', key)
            ";

            var currentCount = await _database.ScriptEvaluateAsync(script,
                new RedisKey[] { _key },
                new RedisValue[] { windowStart });

            return Math.Max(0, _maxRequests - (int)currentCount);
        }

        public async Task<(int count, DateTime oldestRequest)> GetWindowStatsAsync()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)_windowSize.TotalMilliseconds;

            // 清理过期记录并获取统计信息
            const string script = @"
                local key = KEYS[1]
                local window_start = tonumber(ARGV[1])
                
                redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)
                local count = redis.call('ZCARD', key)
                local oldest = nil
                
                if count > 0 then
                    local range = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                    if #range > 0 then
                        oldest = range[2]
                    end
                end
                
                return {count, oldest}
            ";

            var result = await _database.ScriptEvaluateAsync(script,
                new RedisKey[] { _key },
                new RedisValue[] { windowStart });

            if (result.IsNull)
                return (0, DateTime.MinValue);

            var array = (RedisValue[])result!;
            var count = array.Length > 0 && array[0].HasValue ? (int)array[0] : 0;
            var oldestTimestamp = array.Length > 1 && array[1].HasValue ? (long)array[1] : 0;
            var oldestRequest = oldestTimestamp > 0 
                ? DateTimeOffset.FromUnixTimeMilliseconds(oldestTimestamp).DateTime 
                : DateTime.MinValue;

            return (count, oldestRequest);
        }
    }
}
