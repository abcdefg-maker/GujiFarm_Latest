namespace Core.Constants
{
    /// <summary>
    /// 动画系统常量定义
    /// 参数名必须与 Animator Controller 中配置的参数名一致
    /// </summary>
    public static class AnimationConstants
    {
        #region Animator Parameter Names

        /// <summary>
        /// Float参数：移动速度 (0=静止, >0=行走)
        /// </summary>
        public const string ParamSpeed = "Speed";

        /// <summary>
        /// Int参数：朝向方向 (0=正面, 1=背面, 2=侧面)
        /// </summary>
        public const string ParamFacingDirection = "FacingDirection";

        /// <summary>
        /// Bool参数：是否正在使用工具（预留）
        /// </summary>
        public const string ParamIsUsingTool = "IsUsingTool";

        /// <summary>
        /// Int参数：工具类型（预留）
        /// </summary>
        public const string ParamToolType = "ToolType";

        #endregion

        #region Facing Direction Values

        /// <summary>面朝下（面对摄像机）</summary>
        public const int FacingFront = 0;

        /// <summary>面朝上（背对摄像机）</summary>
        public const int FacingBack = 1;

        /// <summary>面朝左或右（翻转由 Skeleton.ScaleX 控制）</summary>
        public const int FacingSide = 2;

        #endregion

        #region Tool Animation Durations

        /// <summary>浇水动画时长（秒），对应 Spine 中 front/back/side_watering</summary>
        public const float WateringDuration = 2.333f;

        /// <summary>锄地动画时长（秒），对应 Spine 中 hoeing</summary>
        public const float HoeingDuration = 2.6f;

        /// <summary>镰刀收割动画时长（秒），对应 Spine 中 sickle</summary>
        public const float SickleDuration = 2.6f;

        #endregion
    }
}
