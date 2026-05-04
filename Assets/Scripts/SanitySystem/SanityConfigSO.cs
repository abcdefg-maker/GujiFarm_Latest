using UnityEngine;

namespace SanitySystem
{
    /// <summary>
    /// SAN系统配置数据。
    /// 定义SAN参数、变异概率曲线、夜间消耗速率等。
    /// 所有曲线均可在 Inspector 中可视化编辑。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSanityConfig", menuName = "Sanity/Sanity Config")]
    public class SanityConfigSO : ScriptableObject
    {
        [Header("SAN值")]
        [Tooltip("SAN最大值")]
        public float maxSanity = SanityConstants.MaxSanity;

        [Tooltip("初始SAN值")]
        public float startingSanity = SanityConstants.DefaultStartingSanity;

        [Header("疯狂症状")]
        [Tooltip("触发症状的SAN间隔（每降低多少点触发一次）")]
        public float symptomInterval = SanityConstants.MadnessSymptomInterval;

        [Header("变异配置")]
        [Tooltip("SAN低于此值才可能出现作物变异")]
        public float mutationStartThreshold = SanityConstants.MutationStartThreshold;

        [Tooltip("变异概率曲线。X轴=归一化SAN(0=疯狂,1=正常), Y轴=变异概率(0~1)")]
        public AnimationCurve mutationProbabilityCurve = new AnimationCurve(
            new Keyframe(0f, 0.8f),    // SAN 0: 80%概率
            new Keyframe(0.5f, 0.2f),  // SAN 50: 20%概率
            new Keyframe(0.8f, 0.05f), // SAN 80: 5%概率
            new Keyframe(1f, 0f)       // SAN 100: 0%概率
        );

        [Header("夜间消耗")]
        [Tooltip("夜间每秒基础SAN消耗")]
        public float nightDrainPerSecond = SanityConstants.DefaultNightDrainPerSecond;

        [Tooltip("消耗倍率曲线。X轴=nightLevel(0~1), Y轴=消耗倍率")]
        public AnimationCurve nightDrainMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(1f, 1f)
        );

        [Header("游戏结束")]
        [Tooltip("SAN归零后到游戏结束画面的延迟（秒）")]
        public float gameOverDelay = 2f;

        #region Public Query Methods

        /// <summary>
        /// 获取指定SAN值对应的变异概率
        /// </summary>
        public float GetMutationProbability(float currentSanity)
        {
            if (currentSanity >= mutationStartThreshold) return 0f;
            float normalized = currentSanity / maxSanity;
            return mutationProbabilityCurve.Evaluate(normalized);
        }

        /// <summary>
        /// 获取指定夜晚程度对应的SAN消耗速率
        /// </summary>
        public float GetNightDrainRate(float nightLevel)
        {
            return nightDrainPerSecond * nightDrainMultiplierCurve.Evaluate(nightLevel);
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            if (maxSanity < 1f) maxSanity = 1f;
            if (startingSanity < 0f) startingSanity = 0f;
            if (startingSanity > maxSanity) startingSanity = maxSanity;
            if (symptomInterval < 1f) symptomInterval = 1f;
            if (mutationStartThreshold < 0f) mutationStartThreshold = 0f;
            if (nightDrainPerSecond < 0f) nightDrainPerSecond = 0f;
            if (gameOverDelay < 0f) gameOverDelay = 0f;
        }

        #endregion
    }
}
