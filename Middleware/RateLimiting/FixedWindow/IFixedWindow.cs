namespace AlgorithmPick.Middleware.RateLimiting.FixedWindow
{
    /// <summary>
    /// 固定窗口计数器接口
    /// </summary>
    public interface IFixedWindow
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
        /// 获取下一个窗口开始时间
        /// </summary>
        /// <returns>下一个窗口开始的Unix时间戳</returns>
        Task<long> GetNextWindowStartAsync();
    }
}
