using TimeSystem;

namespace LightingSystem
{
    /// <summary>
    /// 每帧传递给 IDayNightEffect 的上下文数据。
    /// 使用 struct（值类型）避免每帧堆分配。
    /// </summary>
    public struct DayNightContext
    {
        /// <summary>当前游戏小时 (0 ~ 23.99)</summary>
        public float CurrentHour;

        /// <summary>归一化时间 (0 ~ 1), 0=0:00, 1=24:00</summary>
        public float NormalizedHour;

        /// <summary>夜晚程度 (0=完全白天, 1=完全深夜), 由 nightLevelCurve 计算</summary>
        public float NightLevel;

        /// <summary>当前时段 (Dawn/Morning/Afternoon/Evening/Night)</summary>
        public DayPhase CurrentPhase;

        /// <summary>当前季节</summary>
        public Season CurrentSeason;

        /// <summary>是否处于夜晚时段</summary>
        public bool IsNight;

        /// <summary>本帧 deltaTime</summary>
        public float DeltaTime;
    }

    /// <summary>
    /// 日夜效果插件接口。
    /// 实现此接口的 MonoBehaviour 添加到 DayNightManager 的效果列表后，
    /// 每帧会收到 DayNightContext 并执行自定义逻辑。
    /// 用例：夜晚理智值消耗、屏幕暗角、特殊天气视觉等。
    /// </summary>
    public interface IDayNightEffect
    {
        /// <summary>初始化（DayNightManager 启动后调用一次）</summary>
        void Initialize(DayNightContext initialContext);

        /// <summary>每帧调用，接收当前日夜上下文</summary>
        void Tick(DayNightContext context);

        /// <summary>是否激活。DayNightManager 跳过 IsActive==false 的效果</summary>
        bool IsActive { get; }
    }
}
