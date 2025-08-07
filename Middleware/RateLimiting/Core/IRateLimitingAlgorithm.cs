namespace AlgorithmPick.Middleware.RateLimiting.Core
{
    /// <summary>
    /// 限流算法接口
    /// </summary>
    public interface IRateLimitingAlgorithm
    {
        /// <summary>
        /// 尝试获取令牌/通过限流检查
        /// </summary>
        /// <param name="key">限流标识符（如用户ID、IP地址等）</param>
        /// <param name="cost">消耗的令牌数量，默认为1</param>
        /// <returns>是否通过限流检查</returns>
        Task<bool> TryAcquireAsync(string key, int cost = 1);

        /// <summary>
        /// 获取当前剩余的令牌数量
        /// </summary>
        /// <param name="key">限流标识符</param>
        /// <returns>剩余令牌数量</returns>
        Task<long> GetRemainingTokensAsync(string key);

        /// <summary>
        /// 重置指定key的限流状态
        /// </summary>
        /// <param name="key">限流标识符</param>
        Task ResetAsync(string key);
    }
}
