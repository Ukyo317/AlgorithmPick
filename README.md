# AlgorithmPick - 限流算法演示项目

这个项目演示了在 ASP.NET Core 中实现限流中间件，支持令牌桶和漏桶两种经典的限流算法。

## 功能特性

### 支持的限流算法

1. **令牌桶算法 (Token Bucket)**
   - 允许突发流量
   - 令牌以固定速率补充到桶中
   - 请求需要消耗令牌才能通过
   - 适用于允许短时间内突发请求的场景

2. **漏桶算法 (Leaky Bucket)**
   - 平滑突发流量
   - 请求进入桶中，以恒定速率从桶中漏出
   - 超出容量的请求被丢弃
   - 适用于需要平滑流量的场景

### 中间件特性

- 可配置的限流算法
- 支持自定义键生成器（IP、用户ID、路径等）
- 可配置的响应状态码和消息
- 支持在响应头中包含限流信息
- 详细的日志记录
- 线程安全的实现

## 快速开始

### 1. 基本配置

在 `appsettings.json` 中配置限流参数：

```json
{
  "RateLimit": {
    "Algorithm": "TokenBucket",
    "RequestsPerSecond": 10,
    "Capacity": 20,
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

### 2. 在 Program.cs 中注册服务

```csharp
// 使用配置文件的设置
builder.Services.AddRateLimiting(options =>
{
    var rateLimitSection = builder.Configuration.GetSection("RateLimit");
    // 配置限流选项...
});

// 使用中间件
app.UseRateLimiting();
```

### 3. 使用扩展方法快速配置

```csharp
// 令牌桶算法
builder.Services.AddTokenBucketRateLimiting(
    requestsPerSecond: 10,
    capacity: 20,
    keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
);

// 漏桶算法
builder.Services.AddLeakyBucketRateLimiting(
    requestsPerSecond: 10,
    capacity: 20,
    keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
);
```

## 配置选项

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Algorithm | RateLimitAlgorithm | TokenBucket | 限流算法类型 |
| RequestsPerSecond | int | 10 | 每秒允许的请求数 |
| Capacity | int | 20 | 桶容量 |
| KeyGenerator | Func<HttpContext, string> | IP地址 | 客户端标识生成器 |
| StatusCode | int | 429 | 限流时的HTTP状态码 |
| Message | string | "Too Many Requests" | 限流时的响应消息 |
| IncludeHeaders | bool | true | 是否包含限流响应头 |

## 响应头

当 `IncludeHeaders` 为 true 时，响应会包含以下头部信息：

- `X-RateLimit-Limit`: 限流限制
- `X-RateLimit-Remaining`: 剩余请求数
- `X-RateLimit-Reset`: 重置时间（Unix时间戳）

## API 接口

项目提供了测试接口来验证限流功能：

- `GET /api/RateLimitTest/test` - 测试限流
- `GET /api/RateLimitTest/status` - 获取限流状态
- `POST /api/RateLimitTest/simulate` - 模拟高频请求

## 算法详解

### 令牌桶算法 (Token Bucket)

令牌桶算法的工作原理：

1. 桶中存储令牌，最大容量为 `Capacity`
2. 以固定速率 `RequestsPerSecond` 向桶中添加令牌
3. 每个请求需要消耗一个令牌
4. 当桶中没有足够令牌时，请求被拒绝

**优点：**
- 允许突发流量（在桶容量范围内）
- 在流量平稳时能够"积累"处理能力

**适用场景：**
- API 限流
- 允许偶尔的突发请求
- 需要一定弹性的场景

### 漏桶算法 (Leaky Bucket)

漏桶算法的工作原理：

1. 请求进入桶中排队
2. 桶以固定速率 `RequestsPerSecond` 处理请求
3. 当桶满时，新请求被拒绝

**优点：**
- 强制限制输出速率
- 平滑突发流量

**适用场景：**
- 需要严格控制处理速率
- 保护下游服务
- 网络流量整形

## 自定义键生成器

你可以根据不同的需求自定义键生成器：

```csharp
// 基于IP地址
options.KeyGenerator = context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

// 基于用户ID
options.KeyGenerator = context => context.User.Identity?.Name ?? "anonymous";

// 基于IP和路径
options.KeyGenerator = context => $"{context.Connection.RemoteIpAddress}:{context.Request.Path}";

// 基于API Key
options.KeyGenerator = context => context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "no-key";
```

## 日志记录

中间件会记录详细的日志信息，包括：

- 限流算法的执行情况
- 请求的通过/拒绝状态
- 当前令牌/容量状态

在 `appsettings.json` 中启用调试日志：

```json
{
  "Logging": {
    "LogLevel": {
      "AlgorithmPick.Middleware.RateLimiting": "Debug"
    }
  }
}
```

## 性能考虑

- 使用 `ConcurrentDictionary` 确保线程安全
- 算法实现经过优化，适合高并发场景
- 内存使用效率高，适合长时间运行

## 扩展功能

可以考虑的扩展功能：

1. **Redis 支持**: 在分布式环境中使用 Redis 存储限流状态
2. **滑动窗口算法**: 实现更精确的限流控制
3. **动态配置**: 支持运行时修改限流参数
4. **监控指标**: 集成 Prometheus 等监控系统
5. **白名单/黑名单**: 支持特定客户端的例外处理

## 运行项目

1. 确保安装了 .NET 8.0 SDK
2. 克隆项目到本地
3. 运行命令：
   ```bash
   dotnet run
   ```
4. 打开浏览器访问 `https://localhost:5001`
5. 在主页面测试限流功能

## 许可证

本项目仅供学习和演示使用。
# AlgorithmPick
