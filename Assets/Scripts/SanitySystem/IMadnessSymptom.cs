namespace SanitySystem
{
    /// <summary>
    /// 疯狂症状接口。
    /// 当SAN值跨越10的倍数阈值时触发匹配的症状。
    /// 当前版本只定义接口，具体症状效果后续实现。
    ///
    /// 未来症状示例：
    /// - 屏幕扭曲/色调变化
    /// - 幻听/异常音效
    /// - 幻觉（幻影NPC/怪物）
    /// - 移动抖动/控制异常
    /// </summary>
    public interface IMadnessSymptom
    {
        /// <summary>
        /// 触发阈值。-1=每个阈值都触发，具体值=仅在该阈值触发。
        /// 例: TriggerThreshold=70 表示只在SAN跨过70时触发。
        /// 例: TriggerThreshold=-1 表示跨过任何阈值(90,80,70...)都触发。
        /// </summary>
        int TriggerThreshold { get; }

        /// <summary>优先级（多个症状同时触发时的执行顺序，数值越大越先执行）</summary>
        int Priority { get; }

        /// <summary>是否正在播放</summary>
        bool IsPlaying { get; }

        /// <summary>
        /// SAN跨越阈值时调用
        /// </summary>
        /// <param name="previousSanity">变化前SAN值</param>
        /// <param name="newSanity">变化后SAN值</param>
        /// <param name="thresholdCrossed">跨越的阈值（90/80/70...）</param>
        void OnTrigger(float previousSanity, float newSanity, int thresholdCrossed);

        /// <summary>强制取消症状（如SAN恢复时）</summary>
        void OnCancel();
    }
}
