using AlgorithmPick.Middleware;
using AlgorithmPick.Middleware.RateLimiting.Core;
using AlgorithmPick.Middleware.RateLimiting.TokenBucket;
using AlgorithmPick.Middleware.RateLimiting.LeakyBucket;
using AlgorithmPick.Middleware.RateLimiting.FixedWindow;
using AlgorithmPick.Middleware.RateLimiting.SlidingWindow;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AlgorithmPick.Extensions
{
    /// <summary>
    /// 限流服务扩展方法
    /// </summary>
    public static class RateLimitingExtensions
    {
        /// <summary>
        /// 添加限流服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, Action<RateLimitOptions>? configureOptions = null)
        {
            services.Configure<RateLimitOptions>(options =>
            {
                configureOptions?.Invoke(options);
            });

            // 注册令牌桶工厂
            services.AddSingleton<ITokenBucketFactory>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                
                return options.StorageType switch
                {
                    StorageType.InMemory => new InMemoryTokenBucketFactory(),
                    StorageType.Redis => CreateRedisTokenBucketFactory(serviceProvider, options),
                    _ => throw new ArgumentException($"Unsupported storage type: {options.StorageType}")
                };
            });

            // 注册漏桶工厂
            services.AddSingleton<ILeakyBucketFactory>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                
                return options.StorageType switch
                {
                    StorageType.InMemory => new InMemoryLeakyBucketFactory(),
                    StorageType.Redis => CreateRedisLeakyBucketFactory(serviceProvider, options),
                    _ => throw new ArgumentException($"Unsupported storage type: {options.StorageType}")
                };
            });

            // 注册固定窗口工厂
            services.AddSingleton<IFixedWindowFactory>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                
                return options.StorageType switch
                {
                    StorageType.InMemory => new InMemoryFixedWindowFactory(),
                    StorageType.Redis => CreateRedisFixedWindowFactory(serviceProvider, options),
                    _ => throw new ArgumentException($"Unsupported storage type: {options.StorageType}")
                };
            });

            // 注册滑动窗口工厂
            services.AddSingleton<ISlidingWindowFactory>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                
                return options.StorageType switch
                {
                    StorageType.InMemory => new InMemorySlidingWindowFactory(),
                    StorageType.Redis => CreateRedisSlidingWindowFactory(serviceProvider, options),
                    _ => throw new ArgumentException($"Unsupported storage type: {options.StorageType}")
                };
            });

            // 注册限流算法
            services.AddSingleton<IRateLimitingAlgorithm>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return options.Algorithm switch
                {
                    RateLimitAlgorithm.TokenBucket => new TokenBucketAlgorithm(
                        options.Capacity, 
                        options.RequestsPerSecond, 
                        serviceProvider.GetRequiredService<ITokenBucketFactory>(),
                        loggerFactory.CreateLogger<TokenBucketAlgorithm>()),
                    RateLimitAlgorithm.LeakyBucket => new LeakyBucketAlgorithm(
                        options.Capacity, 
                        options.RequestsPerSecond, 
                        serviceProvider.GetRequiredService<ILeakyBucketFactory>(),
                        loggerFactory.CreateLogger<LeakyBucketAlgorithm>()),
                    RateLimitAlgorithm.FixedWindow => new FixedWindowAlgorithm(
                        options.Capacity,
                        options.WindowSize,
                        serviceProvider.GetRequiredService<IFixedWindowFactory>(),
                        loggerFactory.CreateLogger<FixedWindowAlgorithm>()),
                    RateLimitAlgorithm.SlidingWindow => new SlidingWindowAlgorithm(
                        options.Capacity,
                        options.WindowSize,
                        serviceProvider.GetRequiredService<ISlidingWindowFactory>(),
                        loggerFactory.CreateLogger<SlidingWindowAlgorithm>()),
                    _ => throw new ArgumentException($"Unsupported rate limiting algorithm: {options.Algorithm}")
                };
            });

            return services;
        }

        private static RedisTokenBucketFactory CreateRedisTokenBucketFactory(IServiceProvider serviceProvider, RateLimitOptions options)
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when using Redis storage type.");
            }

            // 注册Redis连接
            var connectionMultiplexer = ConnectionMultiplexer.Connect(options.RedisConnectionString);
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RedisTokenBucket>();
            
            return new RedisTokenBucketFactory(connectionMultiplexer, logger, options.RedisKeyPrefix);
        }

        private static RedisLeakyBucketFactory CreateRedisLeakyBucketFactory(IServiceProvider serviceProvider, RateLimitOptions options)
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when using Redis storage type.");
            }

            // 注册Redis连接
            var connectionMultiplexer = ConnectionMultiplexer.Connect(options.RedisConnectionString);
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RedisLeakyBucket>();
            
            return new RedisLeakyBucketFactory(connectionMultiplexer, logger, options.RedisKeyPrefix.Replace("rate_limit:", "leaky_bucket:"));
        }

        private static RedisFixedWindowFactory CreateRedisFixedWindowFactory(IServiceProvider serviceProvider, RateLimitOptions options)
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when using Redis storage type.");
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(options.RedisConnectionString);
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RedisFixedWindow>();
            
            return new RedisFixedWindowFactory(connectionMultiplexer, logger, options.RedisKeyPrefix.Replace("rate_limit:", "fixed_window:"));
        }

        private static RedisSlidingWindowFactory CreateRedisSlidingWindowFactory(IServiceProvider serviceProvider, RateLimitOptions options)
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is required when using Redis storage type.");
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(options.RedisConnectionString);
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RedisSlidingWindow>();
            
            return new RedisSlidingWindowFactory(connectionMultiplexer, logger, options.RedisKeyPrefix.Replace("rate_limit:", "sliding_window:"));
        }

        /// <summary>
        /// 使用限流中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitingMiddleware>();
        }

        /// <summary>
        /// 添加内存令牌桶限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInMemoryTokenBucketRateLimiting(
            this IServiceCollection services, 
            int requestsPerSecond = 10, 
            int capacity = 20,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.TokenBucket;
                options.StorageType = StorageType.InMemory;
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加Redis令牌桶限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyPrefix">Redis键前缀</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisTokenBucketRateLimiting(
            this IServiceCollection services,
            string redisConnectionString,
            int requestsPerSecond = 10, 
            int capacity = 20,
            string keyPrefix = "rate_limit:",
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.TokenBucket;
                options.StorageType = StorageType.Redis;
                options.RedisConnectionString = redisConnectionString;
                options.RedisKeyPrefix = keyPrefix;
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加内存漏桶限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInMemoryLeakyBucketRateLimiting(
            this IServiceCollection services, 
            int requestsPerSecond = 10, 
            int capacity = 20,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.LeakyBucket;
                options.StorageType = StorageType.InMemory;
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加Redis漏桶限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyPrefix">Redis键前缀</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisLeakyBucketRateLimiting(
            this IServiceCollection services,
            string redisConnectionString,
            int requestsPerSecond = 10, 
            int capacity = 20,
            string keyPrefix = "leaky_bucket:",
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.LeakyBucket;
                options.StorageType = StorageType.Redis;
                options.RedisConnectionString = redisConnectionString;
                options.RedisKeyPrefix = keyPrefix;
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加内存固定窗口限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInMemoryFixedWindowRateLimiting(
            this IServiceCollection services,
            int maxRequests = 100,
            TimeSpan? windowSize = null,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.FixedWindow;
                options.StorageType = StorageType.InMemory;
                options.Capacity = maxRequests;
                options.WindowSize = windowSize ?? TimeSpan.FromMinutes(1);
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加Redis固定窗口限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="keyPrefix">Redis键前缀</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisFixedWindowRateLimiting(
            this IServiceCollection services,
            string redisConnectionString,
            int maxRequests = 100,
            TimeSpan? windowSize = null,
            string keyPrefix = "fixed_window:",
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.FixedWindow;
                options.StorageType = StorageType.Redis;
                options.RedisConnectionString = redisConnectionString;
                options.RedisKeyPrefix = keyPrefix;
                options.Capacity = maxRequests;
                options.WindowSize = windowSize ?? TimeSpan.FromMinutes(1);
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加内存滑动窗口限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInMemorySlidingWindowRateLimiting(
            this IServiceCollection services,
            int maxRequests = 100,
            TimeSpan? windowSize = null,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.SlidingWindow;
                options.StorageType = StorageType.InMemory;
                options.Capacity = maxRequests;
                options.WindowSize = windowSize ?? TimeSpan.FromMinutes(1);
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加Redis滑动窗口限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <param name="maxRequests">窗口内最大请求数</param>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="keyPrefix">Redis键前缀</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisSlidingWindowRateLimiting(
            this IServiceCollection services,
            string redisConnectionString,
            int maxRequests = 100,
            TimeSpan? windowSize = null,
            string keyPrefix = "sliding_window:",
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.SlidingWindow;
                options.StorageType = StorageType.Redis;
                options.RedisConnectionString = redisConnectionString;
                options.RedisKeyPrefix = keyPrefix;
                options.Capacity = maxRequests;
                options.WindowSize = windowSize ?? TimeSpan.FromMinutes(1);
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加漏桶限流（已弃用，请使用AddInMemoryLeakyBucketRateLimiting）
        /// </summary>
        [Obsolete("Please use AddInMemoryLeakyBucketRateLimiting or AddRedisLeakyBucketRateLimiting instead")]
        /// <param name="services">服务集合</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLeakyBucketRateLimiting(
            this IServiceCollection services, 
            int requestsPerSecond = 10, 
            int capacity = 20,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.LeakyBucket;
                options.StorageType = StorageType.InMemory; // 漏桶目前只支持内存
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }
    }
}
