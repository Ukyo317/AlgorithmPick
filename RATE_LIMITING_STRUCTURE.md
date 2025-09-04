# 限流文件结构重组说明

## 重组前问题
原本的 `RateLimiting` 文件夹包含22个文件，全部在同一个目录下，不便于管理和维护。

## 重组后的文件结构

```
Middleware/RateLimiting/
├── Core/                           # 核心接口和配置
│   ├── IRateLimitingAlgorithm.cs  # 主限流算法接口
│   └── RateLimitOptions.cs        # 限流配置选项
├── TokenBucket/                    # 令牌桶算法相关文件
│   ├── ITokenBucket.cs            # 令牌桶接口
│   ├── InMemoryTokenBucket.cs     # 内存令牌桶实现
│   ├── RedisTokenBucket.cs        # Redis令牌桶实现
│   ├── TokenBucketAlgorithm.cs    # 令牌桶算法主类
│   └── TokenBucketFactory.cs      # 令牌桶工厂
├── LeakyBucket/                    # 漏桶算法相关文件
│   ├── ILeakyBucket.cs            # 漏桶接口
│   ├── InMemoryLeakyBucket.cs     # 内存漏桶实现
│   ├── RedisLeakyBucket.cs        # Redis漏桶实现
│   ├── LeakyBucketAlgorithm.cs    # 漏桶算法主类
│   └── LeakyBucketFactory.cs      # 漏桶工厂
├── FixedWindow/                    # 固定窗口算法相关文件
│   ├── IFixedWindow.cs            # 固定窗口接口
│   ├── InMemoryFixedWindow.cs     # 内存固定窗口实现
│   ├── RedisFixedWindow.cs        # Redis固定窗口实现
│   ├── FixedWindowAlgorithm.cs    # 固定窗口算法主类
│   └── FixedWindowFactory.cs      # 固定窗口工厂
└── SlidingWindow/                  # 滑动窗口算法相关文件
    ├── ISlidingWindow.cs          # 滑动窗口接口
    ├── InMemorySlidingWindow.cs   # 内存滑动窗口实现
    ├── RedisSlidingWindow.cs      # Redis滑动窗口实现
    ├── SlidingWindowAlgorithm.cs  # 滑动窗口算法主类
    └── SlidingWindowFactory.cs    # 滑动窗口工厂
```

## 命名空间变更

### 重组前
所有文件都使用 `AlgorithmPick.Middleware.RateLimiting` 命名空间

### 重组后
- Core: `AlgorithmPick.Middleware.RateLimiting.Core`
- TokenBucket: `AlgorithmPick.Middleware.RateLimiting.TokenBucket`
- LeakyBucket: `AlgorithmPick.Middleware.RateLimiting.LeakyBucket`
- FixedWindow: `AlgorithmPick.Middleware.RateLimiting.FixedWindow`
- SlidingWindow: `AlgorithmPick.Middleware.RateLimiting.SlidingWindow`

## 文件更新

### 引用更新的文件
- `Extensions/RateLimitingExtensions.cs` - 添加了所有子命名空间的引用
- `Controllers/ConfigController.cs` - 更新为引用Core命名空间
- `Controllers/HomeController.cs` - 更新为引用Core命名空间  
- `Middleware/RateLimitingMiddleware.cs` - 更新为引用Core命名空间
- `Program.cs` - 更新为引用Core命名空间

### 算法文件内部引用
每个算法的主类都添加了对Core命名空间的引用以使用 `IRateLimitingAlgorithm` 接口

## 重组优势

1. **逻辑分组**: 每个算法的相关文件都在各自的文件夹中
2. **易于维护**: 新增算法时，只需要在对应文件夹中添加文件
3. **清晰结构**: 一目了然地看出项目支持哪些算法
4. **扩展友好**: 为每个算法提供了独立的命名空间，避免命名冲突
5. **代码组织**: 相关功能聚合在一起，提高代码可读性

## 验证
项目构建成功，所有编译错误已修复，文件重组完成。
