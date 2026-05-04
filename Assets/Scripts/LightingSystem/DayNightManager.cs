using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TimeSystem;

namespace LightingSystem
{
    /// <summary>
    /// 日夜光照管理器（单例）。
    /// 每帧根据 TimeManager.CurrentHour 平滑更新场景光照，
    /// 并将 DayNightContext 广播给所有注册的 IDayNightEffect 插件。
    /// </summary>
    public class DayNightManager : MonoBehaviour
    {
        #region Singleton
        public static DayNightManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("光源引用")]
        [Tooltip("场景中的平行光（需设置为 Realtime 或 Mixed 模式）")]
        [SerializeField] private Light directionalLight;

        [Header("光照配置")]
        [Tooltip("默认日夜配置（无季节匹配时使用）")]
        [SerializeField] private DayNightConfigSO defaultConfig;

        [Tooltip("季节专属配置列表（可选，为空则全季节用默认配置）")]
        [SerializeField] private List<DayNightConfigSO> seasonConfigs;

        [Header("屏幕遮罩")]
        [Tooltip("全屏遮罩 Image（放在独立 Canvas 上，Sort Order 低于 UI Canvas）")]
        [SerializeField] private Image screenOverlay;

        [Header("效果插件")]
        [Tooltip("实现 IDayNightEffect 接口的 MonoBehaviour（拖入场景对象）")]
        [SerializeField] private List<MonoBehaviour> effectBehaviours;
        #endregion

        #region Private Fields
        private TimeManager timeManager;
        private DayNightConfigSO activeConfig;
        private List<IDayNightEffect> effects = new List<IDayNightEffect>();
        private bool initialized = false;
        #endregion

        #region Public Properties
        /// <summary>当前夜晚程度 (0=白天, 1=深夜)，供外部系统读取</summary>
        public float CurrentNightLevel { get; private set; }

        /// <summary>当前激活的光照配置</summary>
        public DayNightConfigSO ActiveConfig => activeConfig;
        #endregion

        #region Events
        /// <summary>夜晚程度变化时广播（每帧）</summary>
        public event Action<float> OnNightLevelChanged;

        /// <summary>光照配置切换时广播（季节变化时）</summary>
        public event Action<DayNightConfigSO> OnConfigChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        /// <summary>
        /// 延迟初始化，等一帧确保 TimeManager.Instance 已就绪。
        /// 与 TimeUI、ShopManager 的初始化模式一致。
        /// </summary>
        private IEnumerator DelayedInit()
        {
            yield return null;

            timeManager = TimeManager.Instance;
            if (timeManager == null)
            {
                Debug.LogError("[DayNightManager] 找不到 TimeManager!");
                yield break;
            }

            // 收集效果插件
            CollectEffects();

            // 确定初始配置
            activeConfig = GetConfigForSeason(timeManager.CurrentSeason);
            if (activeConfig == null)
            {
                Debug.LogError("[DayNightManager] 未设置 defaultConfig!");
                yield break;
            }

            // 确保平行光为 Realtime 模式（运行时无法修改 lightmapBakeType，仅打印提示）
            if (directionalLight != null && directionalLight.bakingOutput.lightmapBakeType != LightmapBakeType.Realtime)
            {
                Debug.LogWarning("[DayNightManager] Directional Light 不是 Realtime 模式，请在 Inspector 中修改为 Realtime。");
            }

            // 强制设置环境光模式为 Flat，确保 ambientLight 可写
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            // 订阅季节变化事件
            timeManager.OnSeasonChanged += OnSeasonChanged;

            // 立即同步初始光照状态
            float initHour = GetContinuousHour();
            ApplyLighting(initHour);

            // 初始化所有效果插件
            DayNightContext initialCtx = BuildContext(initHour);
            foreach (var effect in effects)
            {
                effect.Initialize(initialCtx);
            }

            initialized = true;
            Debug.Log($"[DayNightManager] 初始化完成 - 配置: {activeConfig.name}");
        }

        private void Update()
        {
            if (!initialized || timeManager == null || activeConfig == null) return;

            float hour = GetContinuousHour();

            // 每帧平滑更新光照
            ApplyLighting(hour);

            // 构建上下文并 Tick 所有效果插件
            DayNightContext ctx = BuildContext(hour);
            CurrentNightLevel = ctx.NightLevel;
            OnNightLevelChanged?.Invoke(ctx.NightLevel);

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].IsActive)
                {
                    effects[i].Tick(ctx);
                }
            }
        }

        private void OnDestroy()
        {
            if (timeManager != null)
            {
                timeManager.OnSeasonChanged -= OnSeasonChanged;
            }
        }
        #endregion

        #region Time Helpers
        /// <summary>
        /// 获取连续的小时值 (如 6.0, 6.5, 6.99...)。
        /// TimeManager.CurrentHour 每小时跳变 +1，不适合平滑插值。
        /// 通过加入 CurrentMinute/60 得到逐帧连续变化的浮点小时。
        /// </summary>
        private float GetContinuousHour()
        {
            return timeManager.CurrentHour + timeManager.CurrentMinute / 60f;
        }
        #endregion

        #region Lighting Application
        /// <summary>
        /// 根据当前小时评估所有曲线/渐变色，应用到光源和环境设置。
        /// </summary>
        private void ApplyLighting(float hour)
        {
            float t = hour / 24f;

            // 平行光
            if (directionalLight != null)
            {
                directionalLight.intensity = activeConfig.lightIntensityCurve.Evaluate(t);
                directionalLight.color = activeConfig.lightColorGradient.Evaluate(t);

                float pitch = activeConfig.sunPitchCurve.Evaluate(t);
                float yaw = activeConfig.sunYawCurve.Evaluate(t);
                directionalLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // 环境光
            RenderSettings.ambientLight = activeConfig.ambientColorGradient.Evaluate(t);
            RenderSettings.ambientIntensity = activeConfig.ambientIntensityCurve.Evaluate(t);

            // 屏幕遮罩
            if (screenOverlay != null)
            {
                screenOverlay.color = activeConfig.screenOverlayGradient.Evaluate(t);
            }
        }
        #endregion

        #region Context Builder
        private DayNightContext BuildContext(float hour)
        {
            float t = hour / 24f;
            return new DayNightContext
            {
                CurrentHour = hour,
                NormalizedHour = t,
                NightLevel = activeConfig.nightLevelCurve.Evaluate(t),
                CurrentPhase = timeManager.CurrentPhase,
                CurrentSeason = timeManager.CurrentSeason,
                IsNight = timeManager.CurrentPhase == DayPhase.Night,
                DeltaTime = Time.deltaTime
            };
        }
        #endregion

        #region Season Config
        private void OnSeasonChanged(Season season)
        {
            DayNightConfigSO newConfig = GetConfigForSeason(season);
            if (newConfig != null && newConfig != activeConfig)
            {
                activeConfig = newConfig;
                OnConfigChanged?.Invoke(activeConfig);
                Debug.Log($"[DayNightManager] 切换光照配置: {activeConfig.name} (季节: {season})");
            }
        }

        /// <summary>
        /// 查找指定季节的配置。如无匹配，返回默认配置。
        /// </summary>
        private DayNightConfigSO GetConfigForSeason(Season season)
        {
            if (seasonConfigs != null)
            {
                foreach (var cfg in seasonConfigs)
                {
                    if (cfg != null && cfg.targetSeason == season)
                        return cfg;
                }
            }
            return defaultConfig;
        }
        #endregion

        #region Effect Management
        /// <summary>
        /// 从 Inspector 列表中收集实现了 IDayNightEffect 的组件。
        /// </summary>
        private void CollectEffects()
        {
            effects.Clear();
            if (effectBehaviours == null) return;

            foreach (var mb in effectBehaviours)
            {
                if (mb is IDayNightEffect effect)
                {
                    effects.Add(effect);
                }
                else if (mb != null)
                {
                    Debug.LogWarning($"[DayNightManager] {mb.name} 未实现 IDayNightEffect 接口，已忽略。");
                }
            }

            if (effects.Count > 0)
            {
                Debug.Log($"[DayNightManager] 已注册 {effects.Count} 个日夜效果插件。");
            }
        }

        /// <summary>运行时动态注册效果插件</summary>
        public void RegisterEffect(IDayNightEffect effect)
        {
            if (!effects.Contains(effect))
                effects.Add(effect);
        }

        /// <summary>运行时移除效果插件</summary>
        public void UnregisterEffect(IDayNightEffect effect)
        {
            effects.Remove(effect);
        }
        #endregion
    }
}
