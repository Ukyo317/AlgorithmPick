using StackExchange.Redis;

namespace AlgorithmPick.Middleware.RateLimiting.LeakyBucket
{
    /// <summary>
    /// 基于Redis的分布式漏桶实现
    /// </summary>
    public class RedisLeakyBucket : ILeakyBucket
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly int _capacity;
        private readonly double _leakRate;
        private readonly ILogger<RedisLeakyBucket> _logger;

        // Redis键后缀
        private readonly string _volumeKey;
        private readonly string _lastLeakKey;

        public RedisLeakyBucket(
            IDatabase database, 
            string key, 
            int capacity, 
            double leakRate,
            ILogger<RedisLeakyBucket> logger)
        {
            _database = database;
            _key = key;
            _capacity = capacity;
            _leakRate = leakRate;
            _logger = logger;
            
            _volumeKey = $"{key}:volume";
            _lastLeakKey = $"{key}:last_leak";
        }

        public async Task<double> GetCurrentVolumeAsync()
        {
            await LeakAsync();
            var volume = await _database.StringGetAsync(_volumeKey);
            return volume.HasValue ? (double)volume : 0;
        }

        public async Task<bool> TryAddAsync(int amount)
        {
            await LeakAsync();
            
            // 使用Lua脚本确保操作的原子性
            const string script = @"
                local volume_key = KEYS[1]
                local capacity = tonumber(ARGV[1])
                local amount = tonumber(ARGV[2])
                local current_volume = tonumber(redis.call('GET', volume_key) or 0)
                
                if current_volume + amount <= capacity then
                    redis.call('INCRBY', volume_key, amount)
                    return 1
                else
                    return 0
                end
            ";

            var result = await _database.ScriptEvaluateAsync(script, 
                new RedisKey[] { _volumeKey }, 
                new RedisValue[] { _capacity, amount });
                
            return (int)result == 1;
        }

        private async Task LeakAsync()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 使用Lua脚本确保漏出操作的原子性
            const string script = @"
                local volume_key = KEYS[1]
                local last_leak_key = KEYS[2]
                local leak_rate = tonumber(ARGV[1])
                local now = tonumber(ARGV[2])
                
                local last_leak = tonumber(redis.call('GET', last_leak_key) or now)
                local current_volume = tonumber(redis.call('GET', volume_key) or 0)
                
                local time_passed = (now - last_leak) / 1000.0
                
                if time_passed > 0 then
                    local volume_to_leak = time_passed * leak_rate
                    local new_volume = math.max(0, current_volume - volume_to_leak)
                    
                    redis.call('SET', volume_key, new_volume)
                    redis.call('SET', last_leak_key, now)
                    
                    -- 设置过期时间，防止长期不使用的键占用内存
                    redis.call('EXPIRE', volume_key, 3600)  -- 1小时过期
                    redis.call('EXPIRE', last_leak_key, 3600)
                    
                    return new_volume
                end
                
                return current_volume
            ";

            try
            {
                await _database.ScriptEvaluateAsync(script, 
                    new RedisKey[] { _volumeKey, _lastLeakKey }, 
                    new RedisValue[] { _leakRate, now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leak volume for key {Key}", _key);
                throw;
            }
        }
    }
}
