namespace AlgorithmPick.Middleware.RateLimiting.SlidingWindow
{
    /// <summary>
    /// 滑动窗口接口
    /// </summary>
    public interface ISlidingWindow
    {
        /// <summary>
        /// 尝试获取许可
        /// </summary>
        /// <param name="cost">请求消耗的许可数量</param>
        /// <returns>是否成功获取许可</returns>
        Task<bool> TryAcquireAsync(int cost = 1);

        /// <summary>
        /// 获取当前窗口剩余请求数
        /// </summary>
        /// <returns>剩余请求数</returns>
        Task<long> GetRemainingRequestsAsync();

        /// <summary>
        /// 获取窗口内的请求历史
        /// </summary>
        /// <returns>请求历史信息</returns>
        Task<(int count, DateTime oldestRequest)> GetWindowStatsAsync();
    }
}
