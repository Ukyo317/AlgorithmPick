using AlgorithmPick.Middleware.RateLimiting.Core;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AlgorithmPick.Middleware
{
    /// <summary>
    /// 限流中间件
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private readonly IRateLimitingAlgorithm _algorithm;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(
            RequestDelegate next, 
            IOptions<RateLimitOptions> options, 
            IRateLimitingAlgorithm algorithm,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _options = options.Value;
            _algorithm = algorithm;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var key = _options.KeyGenerator(context);
            var allowed = await _algorithm.TryAcquireAsync(key);

            if (_options.IncludeHeaders)
            {
                var remaining = await _algorithm.GetRemainingTokensAsync(key);
                context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerSecond.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(1).ToUnixTimeSeconds().ToString();
            }

            if (!allowed)
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
                
                context.Response.StatusCode = _options.StatusCode;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Rate limit exceeded",
                    message = _options.Message,
                    retryAfter = 1 // 建议1秒后重试
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            await _next(context);
        }
    }
}
