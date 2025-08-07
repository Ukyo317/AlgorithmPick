using AlgorithmPick.Extensions;
using AlgorithmPick.Middleware.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 从配置文件读取限流设置
builder.Services.AddRateLimiting(options =>
{
    var rateLimitSection = builder.Configuration.GetSection("RateLimit");
    
    // 读取算法类型
    if (Enum.TryParse<RateLimitAlgorithm>(rateLimitSection["Algorithm"], out var algorithm))
    {
        options.Algorithm = algorithm;
    }
    
    // 读取其他配置
    options.RequestsPerSecond = rateLimitSection.GetValue<int>("RequestsPerSecond", 10);
    options.Capacity = rateLimitSection.GetValue<int>("Capacity", 20);
    options.StatusCode = rateLimitSection.GetValue<int>("StatusCode", 429);
    options.Message = rateLimitSection.GetValue<string>("Message") ?? "Too Many Requests";
    options.IncludeHeaders = rateLimitSection.GetValue<bool>("IncludeHeaders", true);
    
    // 设置键生成器（基于IP地址）
    options.KeyGenerator = context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
});

// 其他算法配置示例（注释掉的代码）
// 选项1：使用令牌桶算法（允许突发流量）
// builder.Services.AddTokenBucketRateLimiting(
//     requestsPerSecond: 10,  // 每秒10个请求
//     capacity: 20,           // 桶容量20个令牌
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项2：使用漏桶算法（平滑流量）
// builder.Services.AddLeakyBucketRateLimiting(
//     requestsPerSecond: 10,
//     capacity: 20,
//     keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
// );

// 选项3：基于用户和路径的限流
// builder.Services.AddRateLimiting(options =>
// {
//     options.Algorithm = RateLimitAlgorithm.TokenBucket;
//     options.RequestsPerSecond = 5;
//     options.Capacity = 10;
//     options.KeyGenerator = context => $"{context.Connection.RemoteIpAddress}:{context.Request.Path}";
//     options.StatusCode = 429;
//     options.Message = "请求过于频繁，请稍后再试";
//     options.IncludeHeaders = true;
// });

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
