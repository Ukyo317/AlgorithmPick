using AlgorithmPick.Middleware.RateLimiting.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AlgorithmPick.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IOptionsSnapshot<RateLimitOptions> _options;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(IOptionsSnapshot<RateLimitOptions> options, ILogger<ConfigController> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前限流配置
        /// </summary>
        /// <returns></returns>
        [HttpGet("current")]
        public IActionResult GetCurrentConfig()
        {
            var options = _options.Value;
            
            return Ok(new
            {
                algorithm = options.Algorithm.ToString(),
                requestsPerSecond = options.RequestsPerSecond,
                capacity = options.Capacity,
                statusCode = options.StatusCode,
                message = options.Message,
                includeHeaders = options.IncludeHeaders,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 获取算法说明
        /// </summary>
        /// <returns></returns>
        [HttpGet("algorithms")]
        public IActionResult GetAlgorithmDescriptions()
        {
            return Ok(new
            {
                algorithms = new[]
                {
                    new
                    {
                        name = "TokenBucket",
                        displayName = "令牌桶算法",
                        description = "允许突发流量，令牌以固定速率补充",
                        characteristics = new[]
                        {
                            "允许短期内的突发请求",
                            "令牌桶可以预存令牌",
                            "适合API限流场景",
                            "在流量低谷时可以积累处理能力"
                        },
                        useCase = "适用于需要允许偶尔突发请求的场景，如用户API调用"
                    },
                    new
                    {
                        name = "LeakyBucket",
                        displayName = "漏桶算法",
                        description = "以恒定速率处理请求，平滑突发流量",
                        characteristics = new[]
                        {
                            "强制限制输出速率",
                            "平滑处理突发流量",
                            "请求进入队列等待处理",
                            "严格控制处理节奏"
                        },
                        useCase = "适用于需要严格控制处理速率的场景，如保护下游服务"
                    }
                }
            });
        }

        /// <summary>
        /// 获取算法对比
        /// </summary>
        /// <returns></returns>
        [HttpGet("comparison")]
        public IActionResult GetAlgorithmComparison()
        {
            return Ok(new
            {
                comparison = new
                {
                    tokenBucket = new
                    {
                        pros = new[]
                        {
                            "允许突发流量",
                            "用户体验较好",
                            "可以预存处理能力",
                            "适合不规律的流量模式"
                        },
                        cons = new[]
                        {
                            "可能出现瞬间高并发",
                            "对下游系统压力较大",
                            "需要合理设置桶容量"
                        }
                    },
                    leakyBucket = new
                    {
                        pros = new[]
                        {
                            "输出速率恒定",
                            "保护下游系统",
                            "流量整形效果好",
                            "系统负载可预测"
                        },
                        cons = new[]
                        {
                            "无法处理突发流量",
                            "可能增加响应延迟",
                            "用户体验相对较差"
                        }
                    }
                },
                recommendations = new
                {
                    tokenBucket = "推荐用于面向用户的API服务，需要良好用户体验的场景",
                    leakyBucket = "推荐用于内部服务间调用，需要保护下游服务的场景"
                }
            });
        }
    }
}
