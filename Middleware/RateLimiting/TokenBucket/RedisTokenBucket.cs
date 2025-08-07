using StackExchange.Redis;

namespace AlgorithmPick.Middleware.RateLimiting.TokenBucket
{
    /// <summary>
    /// 基于Redis的分布式令牌桶实现
    /// </summary>
    public class RedisTokenBucket : ITokenBucket
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly int _capacity;
        private readonly double _refillRate;
        private readonly ILogger<RedisTokenBucket> _logger;

        // Redis键后缀
        private readonly string _tokensKey;
        private readonly string _lastRefillKey;

        public RedisTokenBucket(
            IDatabase database, 
            string key, 
            int capacity, 
            double refillRate,
            ILogger<RedisTokenBucket> logger)
        {
            _database = database;
            _key = key;
            _capacity = capacity;
            _refillRate = refillRate;
            _logger = logger;
            
            _tokensKey = $"{key}:tokens";
            _lastRefillKey = $"{key}:last_refill";
        }

        public async Task<double> GetCurrentTokensAsync()
        {
            await RefillAsync();
            var tokens = await _database.StringGetAsync(_tokensKey);
            return tokens.HasValue ? (double)tokens : _capacity;
        }

        public async Task<bool> TryConsumeAsync(int cost)
        {
            await RefillAsync();
            
            // 使用Lua脚本确保操作的原子性
            const string script = @"
                local tokens_key = KEYS[1]
                local cost = tonumber(ARGV[1])
                local current_tokens = tonumber(redis.call('GET', tokens_key) or 0)
                
                if current_tokens >= cost then
                    redis.call('DECRBY', tokens_key, cost)
                    return 1
                else
                    return 0
                end
            ";

            var result = await _database.ScriptEvaluateAsync(script, new RedisKey[] { _tokensKey }, new RedisValue[] { cost });
            return (int)result == 1;
        }

        private async Task RefillAsync()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 使用Lua脚本确保令牌补充的原子性
            const string script = @"
                local tokens_key = KEYS[1]
                local last_refill_key = KEYS[2]
                local capacity = tonumber(ARGV[1])
                local refill_rate = tonumber(ARGV[2])
                local now = tonumber(ARGV[3])
                
                local last_refill = tonumber(redis.call('GET', last_refill_key) or now)
                local current_tokens = tonumber(redis.call('GET', tokens_key))
                
                -- 如果是第一次访问，初始化为满容量
                if current_tokens == nil then
                    current_tokens = capacity
                    redis.call('SET', tokens_key, capacity)
                    redis.call('SET', last_refill_key, now)
                    return capacity
                end
                
                local time_passed = (now - last_refill) / 1000.0
                
                if time_passed > 0 then
                    local tokens_to_add = time_passed * refill_rate
                    local new_tokens = math.min(capacity, current_tokens + tokens_to_add)
                    
                    redis.call('SET', tokens_key, new_tokens)
                    redis.call('SET', last_refill_key, now)
                    
                    return new_tokens
                end
                
                return current_tokens
            ";

            try
            {
                await _database.ScriptEvaluateAsync(script, 
                    new RedisKey[] { _tokensKey, _lastRefillKey }, 
                    new RedisValue[] { _capacity, _refillRate, now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refill tokens for key {Key}", _key);
                throw;
            }
        }
    }
}
