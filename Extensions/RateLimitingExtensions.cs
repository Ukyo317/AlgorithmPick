using AlgorithmPick.Middleware;
using AlgorithmPick.Middleware.RateLimiting;
using Microsoft.Extensions.Options;

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

            services.AddSingleton<IRateLimitingAlgorithm>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return options.Algorithm switch
                {
                    RateLimitAlgorithm.TokenBucket => new TokenBucketAlgorithm(
                        options.Capacity, 
                        options.RequestsPerSecond, 
                        loggerFactory.CreateLogger<TokenBucketAlgorithm>()),
                    RateLimitAlgorithm.LeakyBucket => new LeakyBucketAlgorithm(
                        options.Capacity, 
                        options.RequestsPerSecond, 
                        loggerFactory.CreateLogger<LeakyBucketAlgorithm>()),
                    _ => throw new ArgumentException($"Unsupported rate limiting algorithm: {options.Algorithm}")
                };
            });

            return services;
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
        /// 添加令牌桶限流
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="requestsPerSecond">每秒请求数</param>
        /// <param name="capacity">桶容量</param>
        /// <param name="keyGenerator">键生成器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddTokenBucketRateLimiting(
            this IServiceCollection services, 
            int requestsPerSecond = 10, 
            int capacity = 20,
            Func<HttpContext, string>? keyGenerator = null)
        {
            return services.AddRateLimiting(options =>
            {
                options.Algorithm = RateLimitAlgorithm.TokenBucket;
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }

        /// <summary>
        /// 添加漏桶限流
        /// </summary>
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
                options.RequestsPerSecond = requestsPerSecond;
                options.Capacity = capacity;
                if (keyGenerator != null)
                    options.KeyGenerator = keyGenerator;
            });
        }
    }
}
