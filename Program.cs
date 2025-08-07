using AlgorithmPick.Extensions;
using AlgorithmPick.Middleware.RateLimiting.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 配置限流服务 - 支持内存和Redis两种存储方式

// 选项1：从配置文件读取设置（支持动态切换存储类型）
builder.Services.AddRateLimiting(options =>
{
    var rateLimitSection = builder.Configuration.GetSection("RateLimit");
    
    // 读取算法类型
    if (Enum.TryParse<RateLimitAlgorithm>(rateLimitSection["Algorithm"], out var algorithm))
    {
        options.Algorithm = algorithm;
    }
    
    // 读取存储类型
    if (Enum.TryParse<StorageType>(rateLimitSection["StorageType"], out var storageType))
    {
        options.StorageType = storageType;
    }
    
    // Redis配置
    options.RedisConnectionString = rateLimitSection["RedisConnectionString"];
    options.RedisKeyPrefix = rateLimitSection["RedisKeyPrefix"] ?? "rate_limit:";
    
    // 读取其他配置
    options.RequestsPerSecond = rateLimitSection.GetValue<int>("RequestsPerSecond", 10);
    options.Capacity = rateLimitSection.GetValue<int>("Capacity", 20);
    
    // 读取窗口大小（用于固定窗口和滑动窗口算法）
    var windowSizeString = rateLimitSection["WindowSize"];
    if (!string.IsNullOrEmpty(windowSizeString) && TimeSpan.TryParse(windowSizeString, out var windowSize))
    {
        options.WindowSize = windowSize;
    }
    options.StatusCode = rateLimitSection.GetValue<int>("StatusCode", 429);
    options.Message = rateLimitSection.GetValue<string>("Message") ?? "Too Many Requests";
    options.IncludeHeaders = rateLimitSection.GetValue<bool>("IncludeHeaders", true);
    
    // 设置键生成器（基于IP地址）
    options.KeyGenerator = context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
});

// 选项2：内存令牌桶算法（适用于单机部署）
// builder.Services.AddInMemoryTokenBucketRateLimiting(
//     requestsPerSecond: 10,  // 每秒10个请求
//     capacity: 20,           // 桶容量20个令牌
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项3：Redis令牌桶算法（适用于分布式部署）
// builder.Services.AddRedisTokenBucketRateLimiting(
//     redisConnectionString: "localhost:6379",
//     requestsPerSecond: 10,
//     capacity: 20,
//     keyPrefix: "rate_limit:",
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项4：内存漏桶算法（平滑流量）
// builder.Services.AddInMemoryLeakyBucketRateLimiting(
//     requestsPerSecond: 10,
//     capacity: 20,
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项6：Redis滑动窗口算法（分布式平滑流量）
// builder.Services.AddRedisSlidingWindowRateLimiting(
//     redisConnectionString: "localhost:6379",
//     maxRequests: 100,
//     windowSize: TimeSpan.FromMinutes(1),
//     keyPrefix: "sliding_window:",
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项7：内存固定窗口算法
// builder.Services.AddInMemoryFixedWindowRateLimiting(
//     maxRequests: 100,
//     windowSize: TimeSpan.FromMinutes(1),
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项8：Redis固定窗口算法（分布式）
// builder.Services.AddRedisFixedWindowRateLimiting(
//     redisConnectionString: "localhost:6379",
//     maxRequests: 100,
//     windowSize: TimeSpan.FromMinutes(1),
//     keyPrefix: "fixed_window:",
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项9：内存滑动窗口算法（精确限流）
// builder.Services.AddInMemorySlidingWindowRateLimiting(
//     maxRequests: 100,
//     windowSize: TimeSpan.FromMinutes(1),
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项10：基于用户和路径的分布式限流示例
// builder.Services.AddRedisTokenBucketRateLimiting(
//     redisConnectionString: "localhost:6379",
//     requestsPerSecond: 5,
//     capacity: 10,
//     keyGenerator: context => $"{context.Connection.RemoteIpAddress}:{context.Request.Path}"
// );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 使用限流中间件（放在路由之前）
app.UseRateLimiting();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
