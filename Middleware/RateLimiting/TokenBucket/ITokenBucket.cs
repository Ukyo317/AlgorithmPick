namespace AlgorithmPick.Middleware.RateLimiting.TokenBucket
{
    /// <summary>
    /// 令牌桶接口
    /// </summary>
    public interface ITokenBucket
    {
        /// <summary>
        /// 当前令牌数量
        /// </summary>
        Task<double> GetCurrentTokensAsync();

        /// <summary>
        /// 尝试消费指定数量的令牌
        /// </summary>
        /// <param name="cost">要消费的令牌数量</param>
        /// <returns>是否成功消费</returns>
        Task<bool> TryConsumeAsync(int cost);
    }
}
