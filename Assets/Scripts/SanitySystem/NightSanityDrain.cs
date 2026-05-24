using UnityEngine;
using LightingSystem;

namespace SanitySystem
{
    /// <summary>
    /// 夜晚SAN消耗。
    /// 同时实现 ISanityDrainSource（提供消耗数据）
    /// 和 IDayNightEffect（接收夜晚程度）。
    ///
    /// 这是 IDayNightEffect 接口注释中提到的用例：
    /// "用例：夜晚理智值消耗、屏幕暗角、特殊天气视觉等。"
    /// </summary>
    public class NightSanityDrain : MonoBehaviour, ISanityDrainSource, IDayNightEffect
    {
        #region Private Fields
        private float currentNightLevel = 0f;
        private float nightLevelThreshold = SanityConstants.NightDrainActivationLevel;
        private SanityConfigSO config;
        private bool initialized = false;

        // [TEMP DEBUG] 临时调试 — 修复完成后删除
        private float debugLogTimer = 0f;
        private int debugTickCount = 0;
        #endregion

        #region ISanityDrainSource Implementation

        public string SourceID => "NightDrain";
        public bool IsActive => initialized && currentNightLevel > nightLevelThreshold;

        public float GetDrainAmount(float deltaTime, float currentSanity)
        {
            if (config == null) return 0f;
            return config.GetNightDrainRate(currentNightLevel) * deltaTime;
        }

        #endregion

        #region IDayNightEffect Implementation

        /// <summary>始终激活，持续接收夜晚程度更新</summary>
        bool IDayNightEffect.IsActive => true;

        public void Initialize(DayNightContext initialContext)
        {
            currentNightLevel = initialContext.NightLevel;
        }

        public void Tick(DayNightContext context)
        {
            currentNightLevel = context.NightLevel;

            // [TEMP DEBUG] 临时调试 — 修复完成后删除
            debugTickCount++;
            debugLogTimer += context.DeltaTime;
            if (debugLogTimer >= 2f)
            {
                debugLogTimer = 0f;
                bool drainActive = initialized && currentNightLevel > nightLevelThreshold;
                Debug.Log($"[NightSanityDrain DEBUG] hour={context.CurrentHour:F2} nightLevel={currentNightLevel:F2} threshold={nightLevelThreshold} initialized={initialized} IsActive={drainActive} ticksSinceStart={debugTickCount} SAN={SanityManager.Instance?.CurrentSanity ?? -1f:F1}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private System.Collections.IEnumerator DelayedInit()
        {
            yield return null;

            // 获取配置
            if (SanityManager.Instance == null)
            {
                Debug.LogWarning("[NightSanityDrain] 找不到 SanityManager，夜晚消耗已禁用。");
                yield break;
            }

            // 从SanityManager获取配置（通过公共属性）
            config = GetConfig();
            if (config == null)
            {
                Debug.LogWarning("[NightSanityDrain] 找不到 SanityConfigSO 配置。");
                yield break;
            }

            // 注册到 SanityManager
            SanityManager.Instance.RegisterDrainSource(this);

            // 注册到 DayNightManager
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.RegisterEffect(this);
            }
            else
            {
                Debug.LogWarning("[NightSanityDrain] 找不到 DayNightManager!");
            }

            initialized = true;
            Debug.Log("[NightSanityDrain] 初始化完成");
        }

        private void OnDestroy()
        {
            if (SanityManager.Instance != null)
            {
                SanityManager.Instance.UnregisterDrainSource(this);
            }

            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.UnregisterEffect(this);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 从 SanityManager 获取配置
        /// </summary>
        private SanityConfigSO GetConfig()
        {
            return SanityManager.Instance != null ? SanityManager.Instance.Config : null;
        }

        #endregion
    }
}
