# 窗口算法详解

## 固定窗口计数器算法 (Fixed Window Counter)

### 算法原理
固定窗口计数器算法将时间分割成固定大小的窗口，在每个窗口内统计请求数量。当请求数量超过阈值时，拒绝后续请求。

### 优点
- **实现简单**: 逻辑清晰，易于理解和实现
- **内存占用低**: 每个key只需要存储一个计数器
- **性能高**: 操作复杂度为O(1)

### 缺点
- **边界突发**: 在窗口边界可能出现双倍流量
- **不够平滑**: 流量控制不够精确

### 使用场景
- API请求限流
- 简单的速率控制
- 对精确度要求不高的场景

### 配置示例
```json
{
  "RateLimit": {
    "Algorithm": "FixedWindow",
    "StorageType": "Redis",
    "Capacity": 100,
    "WindowSize": "00:01:00"
  }
}
```

### 代码示例
```csharp
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
```

---

## 滑动窗口算法 (Sliding Window)

### 算法原理
滑动窗口算法维护一个时间窗口，该窗口随时间滑动。算法记录每个请求的时间戳，在处理新请求时移除过期的请求记录。

### 优点
- **精确控制**: 避免固定窗口的边界突发问题
- **平滑限流**: 提供更平滑的流量控制
- **实时响应**: 能够实时反映当前的请求状态

### 缺点
- **内存占用高**: 需要存储窗口内所有请求的时间戳
- **性能开销**: 需要定期清理过期记录
- **复杂度高**: 实现相对复杂

### 使用场景
- 精确的API限流
- 高价值资源保护
- 需要平滑流量控制的场景

### 实现细节

#### 内存实现
使用队列存储请求时间戳：
```csharp
private readonly Queue<DateTime> _requestTimes = new();
```

#### Redis实现
使用Sorted Set存储请求时间戳：
```lua
-- 清理过期请求
redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)

-- 检查当前请求数
local current_count = redis.call('ZCARD', key)

-- 添加新请求
redis.call('ZADD', key, now, now)
```

### 配置示例
```json
{
  "RateLimit": {
    "Algorithm": "SlidingWindow",
    "StorageType": "Redis",
    "Capacity": 100,
    "WindowSize": "00:01:00"
  }
}
```

### 代码示例
```csharp
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

---

## 算法对比

| 特性 | 固定窗口 | 滑动窗口 |
|------|----------|----------|
| **精确度** | 中等 | 高 |
| **内存占用** | 低 | 高 |
| **实现复杂度** | 简单 | 复杂 |
| **边界突发** | 有 | 无 |
| **性能** | 高 | 中等 |
| **适用场景** | 简单限流 | 精确限流 |

## 选择建议

### 选择固定窗口当：
- 对性能要求高
- 内存资源有限
- 可以接受边界突发
- 实现简单性优先

### 选择滑动窗口当：
- 需要精确的流量控制
- 不能接受突发流量
- 内存资源充足
- 用户体验优先

## 性能优化

### 固定窗口优化
1. **批量过期**: 设置合适的Redis过期时间
2. **预分片**: 对于高并发场景可以考虑分片
3. **缓存预热**: 预先创建窗口键

### 滑动窗口优化
1. **定时清理**: 定期清理过期的请求记录
2. **批量操作**: 使用Lua脚本进行批量操作
3. **内存控制**: 设置最大窗口大小防止内存溢出

## 监控和调试

### 关键指标
- 窗口内请求数量
- 被拒绝的请求数量
- 平均响应时间
- 内存使用情况

### 日志示例
```
[14:30:15 DBG] FixedWindow - Key: 192.168.1.100, Cost: 1, Success: True, Remaining: 95, NextWindow: 2025-08-07T14:31:00Z
[14:30:16 DBG] SlidingWindow - Key: 192.168.1.100, Cost: 1, Success: True, Remaining: 94, WindowCount: 6, OldestRequest: 2025-08-07T14:29:20Z
```
