namespace SanitySystem
{
    /// <summary>
    /// SAN系统常量，集中管理所有默认值
    /// </summary>
    public static class SanityConstants
    {
        #region SAN值

        /// <summary>SAN最大值</summary>
        public const float MaxSanity = 100f;

        /// <summary>初始SAN值</summary>
        public const float DefaultStartingSanity = 100f;

        /// <summary>游戏结束阈值</summary>
        public const float GameOverThreshold = 0f;

        /// <summary>疯狂症状触发间隔（每降10点）</summary>
        public const float MadnessSymptomInterval = 10f;

        #endregion

        #region 变异

        /// <summary>SAN低于此值才可能出现作物变异</summary>
        public const float MutationStartThreshold = 80f;

        /// <summary>SAN=0时的最大变异概率</summary>
        public const float MaxMutationProbability = 0.8f;

        #endregion

        #region 夜晚消耗

        /// <summary>夜间每秒基础SAN消耗</summary>
        public const float DefaultNightDrainPerSecond = 0.5f;

        /// <summary>夜晚程度高于此值时开始消耗</summary>
        public const float NightDrainActivationLevel = 0.5f;

        #endregion
    }
}
