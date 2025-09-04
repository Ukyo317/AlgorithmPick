using Microsoft.AspNetCore.Mvc;

namespace AlgorithmPick.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitTestController : ControllerBase
    {
        private readonly ILogger<RateLimitTestController> _logger;

        public RateLimitTestController(ILogger<RateLimitTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 测试限流的简单接口
        /// </summary>
        /// <returns></returns>
        [HttpGet("test")]
        public IActionResult Test()
        {
            var timestamp = DateTime.UtcNow;
            _logger.LogInformation("Rate limit test endpoint called at {Timestamp}", timestamp);
            
            return Ok(new
            {
                message = "Request successful",
                timestamp = timestamp,
                requestId = HttpContext.TraceIdentifier
            });
        }

        /// <summary>
        /// 获取当前限流状态
        /// </summary>
        /// <returns></returns>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var headers = Response.Headers;
            
            return Ok(new
            {
                message = "Rate limit status",
                headers = new
                {
                    limit = headers.ContainsKey("X-RateLimit-Limit") ? headers["X-RateLimit-Limit"].ToString() : null,
                    remaining = headers.ContainsKey("X-RateLimit-Remaining") ? headers["X-RateLimit-Remaining"].ToString() : null,
                    reset = headers.ContainsKey("X-RateLimit-Reset") ? headers["X-RateLimit-Reset"].ToString() : null
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 模拟高频请求的接口
        /// </summary>
        /// <returns></returns>
        [HttpPost("simulate")]
        public async Task<IActionResult> SimulateHighFrequency()
        {
            var results = new List<object>();
            var httpClient = new HttpClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // 连续发送20个请求来测试限流
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var response = await httpClient.GetAsync($"{baseUrl}/api/RateLimitTest/test");
                    results.Add(new
                    {
                        requestNumber = i + 1,
                        statusCode = (int)response.StatusCode,
                        success = response.IsSuccessStatusCode,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        requestNumber = i + 1,
                        error = ex.Message,
                        timestamp = DateTime.UtcNow
                    });
                }

                // 短暂延迟
                await Task.Delay(100);
            }

            return Ok(new
            {
                message = "Simulation completed",
                totalRequests = results.Count,
                results = results
            });
        }
    }
}
