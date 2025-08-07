using System.Diagnostics;
using AlgorithmPick.Models;
using AlgorithmPick.Middleware.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace AlgorithmPick.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRateLimitingAlgorithm _rateLimitingAlgorithm;

        public HomeController(ILogger<HomeController> logger, IRateLimitingAlgorithm rateLimitingAlgorithm)
        {
            _logger = logger;
            _rateLimitingAlgorithm = rateLimitingAlgorithm;
        }

        public async Task<IActionResult> Index()
        {
            // 获取当前用户的限流状态
            var clientKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var remainingTokens = await _rateLimitingAlgorithm.GetRemainingTokensAsync(clientKey);
            
            ViewBag.RemainingTokens = remainingTokens;
            ViewBag.ClientKey = clientKey;
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Config()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
