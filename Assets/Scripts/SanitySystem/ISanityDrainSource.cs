namespace SanitySystem
{
    /// <summary>
    /// SAN持续消耗来源接口。
    /// 实现此接口并注册到 SanityManager 后，每帧会被调用获取消耗量。
    /// 一次性消耗（如事件触发）直接调用 SanityManager.DrainSanity() 即可，
    /// 无需实现此接口。
    /// </summary>
    public interface ISanityDrainSource
    {
        /// <summary>来源标识（调试/UI用）</summary>
        string SourceID { get; }

        /// <summary>是否正在激活</summary>
        bool IsActive { get; }

        /// <summary>
        /// 每帧调用，返回本帧的消耗量。
        /// 正值=消耗SAN，负值=恢复SAN。
        /// </summary>
        float GetDrainAmount(float deltaTime, float currentSanity);
    }
}
