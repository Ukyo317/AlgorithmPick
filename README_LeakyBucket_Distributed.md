# 分布式漏桶算法使用示例

## 快速开始

### 1. 配置文件方式（推荐）

在 `appsettings.json` 中配置：

```json
{
  "RateLimit": {
    "Algorithm": "LeakyBucket",
    "StorageType": "Redis",
    "RedisConnectionString": "localhost:6379",
    "RedisKeyPrefix": "leaky_bucket:",
    "RequestsPerSecond": 10,
    "Capacity": 20,
    "StatusCode": 429,
    "Message": "请求过于频繁，请稍后再试",
    "IncludeHeaders": true
  }
}
```

在 `Program.cs` 中注册服务：

```csharp
builder.Services.AddRateLimiting(); // 使用配置文件中的设置
app.UseRateLimiting();
```

### 2. 代码配置方式

#### 内存漏桶（单机）
```csharp
builder.Services.AddInMemoryLeakyBucketRateLimiting(
    requestsPerSecond: 10,
    capacity: 20,
    keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
);
```

#### Redis漏桶（分布式）
```csharp
builder.Services.AddRedisLeakyBucketRateLimiting(
    redisConnectionString: "localhost:6379",
    requestsPerSecond: 10,
    capacity: 20,
    keyPrefix: "leaky_bucket:",
    keyGenerator: context => context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
);
```

## 分布式漏桶算法的技术特点

### 1. Redis实现原理

分布式漏桶通过Redis存储两个关键信息：
- `{key}:volume` - 当前桶中的容量
- `{key}:last_leak` - 上次漏出的时间戳

### 2. 原子性保证

使用Lua脚本确保操作的原子性：

```lua
-- 漏出操作
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
    redis.call('EXPIRE', volume_key, 3600)
    redis.call('EXPIRE', last_leak_key, 3600)
    
    return new_volume
end
```

### 3. 自动内存管理

- 设置Redis键1小时的过期时间
- 防止长期不使用的键占用内存
- 自动清理无用的限流状态

## 算法对比

| 特性 | 内存漏桶 | Redis漏桶 |
|------|----------|-----------|
| 部署方式 | 单机 | 分布式 |
| 性能 | 最高 | 高（网络延迟） |
| 状态一致性 | 仅本机 | 全局一致 |
| 扩展性 | 有限 | 优秀 |
| 复杂度 | 低 | 中等 |

## 使用场景

### 内存漏桶适用于：
- 单机部署的应用
- 对性能要求极高的场景
- 不需要跨实例状态同步

### Redis漏桶适用于：
- 微服务架构
- 负载均衡环境
- 需要全局限流的场景
- Kubernetes等容器化环境

## 监控和调试

启用调试日志来监控漏桶状态：

```json
{
  "Logging": {
    "LogLevel": {
      "AlgorithmPick.Middleware.RateLimiting": "Debug"
    }
  }
}
```

日志示例：
```
[14:30:15 DBG] LeakyBucket - Key: 192.168.1.100, Cost: 1, Success: True, Current: 5.2
```

## 性能优化建议

1. **Redis连接池**: 使用连接池复用Redis连接
2. **键过期策略**: 根据业务需求调整过期时间
3. **批量操作**: 对于高频场景考虑批量处理
4. **监控**: 监控Redis性能和网络延迟
