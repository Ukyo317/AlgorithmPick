# 限流算法配置示例

## 1. 内存令牌桶配置（单机部署）
```json
{
  "RateLimit": {
    "Algorithm": "TokenBucket",
    "StorageType": "InMemory",
    "RequestsPerSecond": 10,
    "Capacity": 20,
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 2. Redis令牌桶配置（分布式部署）
```json
{
  "RateLimit": {
    "Algorithm": "TokenBucket",
    "StorageType": "Redis",
    "RedisConnectionString": "localhost:6379",
    "RedisKeyPrefix": "rate_limit:",
    "RequestsPerSecond": 10,
    "Capacity": 20,
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 3. 内存漏桶配置
```json
{
  "RateLimit": {
    "Algorithm": "LeakyBucket",
    "StorageType": "InMemory",
    "RequestsPerSecond": 10,
    "Capacity": 20,
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 5. 内存固定窗口配置
```json
{
  "RateLimit": {
    "Algorithm": "FixedWindow",
    "StorageType": "InMemory",
    "Capacity": 100,
    "WindowSize": "00:01:00",
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 6. Redis固定窗口配置（分布式）
```json
{
  "RateLimit": {
    "Algorithm": "FixedWindow",
    "StorageType": "Redis",
    "RedisConnectionString": "localhost:6379",
    "RedisKeyPrefix": "fixed_window:",
    "Capacity": 100,
    "WindowSize": "00:01:00",
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 7. 内存滑动窗口配置
```json
{
  "RateLimit": {
    "Algorithm": "SlidingWindow",
    "StorageType": "InMemory",
    "Capacity": 100,
    "WindowSize": "00:01:00",
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 8. Redis滑动窗口配置（分布式）
```json
{
  "RateLimit": {
    "Algorithm": "SlidingWindow",
    "StorageType": "Redis",
    "RedisConnectionString": "localhost:6379",
    "RedisKeyPrefix": "sliding_window:",
    "Capacity": 100,
    "WindowSize": "00:01:00",
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

## 配置说明

- **Algorithm**: 限流算法类型 (TokenBucket, LeakyBucket, FixedWindow, SlidingWindow)
- **StorageType**: 存储类型 (InMemory, Redis)
- **RedisConnectionString**: Redis连接字符串 (仅当StorageType为Redis时需要)
- **RedisKeyPrefix**: Redis键前缀
- **RequestsPerSecond**: 每秒允许的请求数 (仅用于令牌桶和漏桶算法)
- **Capacity**: 桶容量或窗口内最大请求数
- **WindowSize**: 窗口大小 (仅用于固定窗口和滑动窗口算法，格式: "HH:mm:ss")
- **StatusCode**: 限流时返回的HTTP状态码
- **Message**: 限流时返回的消息
- **IncludeHeaders**: 是否在响应头中包含限流信息

## 算法对比

### 令牌桶算法 (TokenBucket)
- **特点**: 允许突发流量，适用于需要处理短时间高并发的场景
- **适用场景**: API网关、Web应用
- **支持存储**: 内存、Redis

### 漏桶算法 (LeakyBucket)
- **特点**: 平滑处理请求，恒定速率输出
- **适用场景**: 需要严格控制流量速率的场景
- **支持存储**: 内存、Redis

### 固定窗口算法 (FixedWindow)
- **特点**: 在固定时间窗口内限制请求数量，实现简单
- **适用场景**: 需要简单限流控制的场景
- **支持存储**: 内存、Redis
- **注意**: 存在窗口边界突发流量问题

### 滑动窗口算法 (SlidingWindow)
- **特点**: 精确的流量控制，避免固定窗口的突发问题
- **适用场景**: 需要精确限流控制的场景
- **支持存储**: 内存、Redis
- **注意**: 内存占用相对较高

## 分布式漏桶算法的实现原理

### Redis实现要点
1. **状态存储**: 使用两个Redis键存储桶的容量和上次漏出时间
2. **原子性保证**: 使用Lua脚本确保漏出和添加操作的原子性
3. **自动过期**: 设置Redis键的过期时间，防止内存泄漏
4. **一致性**: 保证分布式环境下所有实例的漏出速率一致

## 部署方式选择

### 单机部署
推荐使用 `StorageType: "InMemory"`, 性能最佳。

### 分布式部署
必须使用 `StorageType: "Redis"`, 确保多个实例间限流状态一致。

## 代码配置示例

### 内存存储配置
```csharp
// 内存令牌桶
services.AddInMemoryTokenBucketRateLimiting(requestsPerSecond: 10, capacity: 20);

// 内存漏桶
services.AddInMemoryLeakyBucketRateLimiting(requestsPerSecond: 10, capacity: 20);
```

### Redis分布式配置
```csharp
// Redis令牌桶
services.AddRedisTokenBucketRateLimiting(
    redisConnectionString: "localhost:6379",
    requestsPerSecond: 10,
    capacity: 20
);

// Redis漏桶
services.AddRedisLeakyBucketRateLimiting(
    redisConnectionString: "localhost:6379",
    requestsPerSecond: 10,
    capacity: 20
);

// 内存固定窗口
services.AddInMemoryFixedWindowRateLimiting(
    maxRequests: 100,
    windowSize: TimeSpan.FromMinutes(1)
);

// Redis固定窗口
services.AddRedisFixedWindowRateLimiting(
    redisConnectionString: "localhost:6379",
    maxRequests: 100,
    windowSize: TimeSpan.FromMinutes(1)
);

// 内存滑动窗口
services.AddInMemorySlidingWindowRateLimiting(
    maxRequests: 100,
    windowSize: TimeSpan.FromMinutes(1)
);

// Redis滑动窗口
services.AddRedisSlidingWindowRateLimiting(
    redisConnectionString: "localhost:6379",
    maxRequests: 100,
    windowSize: TimeSpan.FromMinutes(1)
);
```
