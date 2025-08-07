namespace AlgorithmPick.Middleware.RateLimiting.LeakyBucket
{
    /// <summary>
    /// 漏桶接口
    /// </summary>
    public interface ILeakyBucket
    {
        /// <summary>
        /// 当前桶中的容量
        /// </summary>
        Task<double> GetCurrentVolumeAsync();

        /// <summary>
        /// 尝试向桶中添加指定数量的请求
        /// </summary>
        /// <param name="amount">要添加的请求数量</param>
        /// <returns>是否成功添加</returns>
        Task<bool> TryAddAsync(int amount);
    }
}
