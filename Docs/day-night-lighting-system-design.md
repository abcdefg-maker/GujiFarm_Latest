# 昼夜光照系统 - 策划文档

> **项目**: GuJi_Farm_Project
> **Unity 版本**: 2022.3.62f3c1 (LTS)
> **日期**: 2026-02-27
> **版本**: v1.0
> **依赖**: TimeSystem (TimeManager)

---

## 目录

1. [系统概述](#1-系统概述)
2. [技术方案](#2-技术方案)
3. [系统架构设计](#3-系统架构设计)
4. [详细功能设计](#4-详细功能设计)
5. [光照参数配置表](#5-光照参数配置表)
6. [季节差异化方案](#6-季节差异化方案)
7. [可扩展性设计](#7-可扩展性设计)
8. [与TimeManager集成](#8-与timemanager集成)
9. [Unity Editor配置步骤](#9-unity-editor配置步骤)
10. [开发路线图](#10-开发路线图)

---

## 1. 系统概述

### 1.1 设计目标

基于现有的 `TimeManager` 时间系统，实现**场景光照随游戏时间平滑变化**的昼夜循环效果，让玩家感知到自然的日出、正午、日落、夜晚氛围变化。

### 1.2 核心特性

- **平滑过渡**：光照参数逐帧连续变化，无跳变
- **数据驱动**：所有光照参数通过 ScriptableObject 配置，设计师可在 Inspector 中可视化调节曲线和渐变色
- **季节适配**：不同季节可使用不同的光照配置（可选）
- **可扩展**：通过 `IDayNightEffect` 接口，未来可插入任意昼夜效果（理智值消耗、屏幕暗角、特殊天气等），核心系统无需修改

### 1.3 设计约束

| 约束项 | 现状 | 说明 |
|--------|------|------|
| 渲染管线 | Built-in (Legacy) Forward | 不使用 URP/HDRP API |
| 后处理 | 未安装任何后处理包 | 仅使用 Light + RenderSettings API |
| 色彩空间 | Gamma | `LightsUseLinearIntensity = 0` |
| 现有光源 | 1个 Directional Light（Baked模式） | 需改为 Realtime 模式 |
| 环境光模式 | Skybox | 需改为 Flat(Color) 模式以支持脚本控制 |

---

## 2. 技术方案

### 2.1 Built-in 管线下可控制的光照参数

| 参数 | API | 控制方式 | 视觉效果 |
|------|-----|----------|----------|
| 平行光强度 | `light.intensity` | AnimationCurve | 整体场景明暗 |
| 平行光颜色 | `light.color` | Gradient | 日出金黄、正午白、日落橘红、夜晚蓝 |
| 太阳仰角 | `light.transform.rotation` (X轴) | AnimationCurve | 影子长短和方向变化 |
| 太阳水平角 | `light.transform.rotation` (Y轴) | AnimationCurve | 光照方向（通常固定） |
| 环境光颜色 | `RenderSettings.ambientLight` | Gradient | 阴影区域的环境色调 |
| 环境光强度 | `RenderSettings.ambientIntensity` | AnimationCurve | 暗处的整体亮度 |

### 2.2 平滑过渡方案

参考现有 `ClockUI.cs` 的实现模式：在 `Update()` 中直接读取 `TimeManager.Instance.CurrentHour`，将其归一化为 `0~1` 后评估 `AnimationCurve` 和 `Gradient`。

由于 `CurrentHour` 本身就是每帧连续递增的浮点数（精度取决于 `Time.deltaTime`），所以曲线评估的结果天然就是平滑的，**无需额外的插值/Lerp处理**。

```
每帧执行:
  normalizedTime = TimeManager.Instance.CurrentHour / 24f   // 0.0 ~ 1.0
  light.intensity = intensityCurve.Evaluate(normalizedTime)  // 连续平滑
  light.color     = colorGradient.Evaluate(normalizedTime)   // 连续平滑
```

### 2.3 关于 AmbientMode

Built-in 管线中，`RenderSettings.ambientLight` 的写入**仅在 `AmbientMode.Flat` 模式下生效**。当前场景使用的是 `Skybox` 模式，脚本写入 `ambientLight` 会被静默忽略。

**解决方案**：在 `DayNightManager` 初始化时强制设置：
```csharp
RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
```

---

## 3. 系统架构设计

### 3.1 三层架构

```
┌──────────────────────────────────────────────────────────────┐
│                  TimeManager (现有，不修改)                   │
│  提供: CurrentHour, CurrentPhase, CurrentSeason              │
│  事件: OnSeasonChanged                                       │
└────────────────────────┬─────────────────────────────────────┘
                         │ 轮询 CurrentHour (每帧)
                         │ 订阅 OnSeasonChanged (切换配置)
┌────────────────────────▼─────────────────────────────────────┐
│              DayNightManager (新建，核心驱动器)                │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  职责:                                                │    │
│  │  1. 每帧读取时间，评估曲线，应用光照参数              │    │
│  │  2. 构建 DayNightContext 上下文                       │    │
│  │  3. Tick 所有注册的 IDayNightEffect                   │    │
│  │  4. 季节变化时切换光照配置 SO                         │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  持有: DayNightConfigSO (光照曲线/渐变色数据)                │
│  持有: List<IDayNightEffect> (效果插件列表)                  │
│  广播: OnNightLevelChanged(float)                            │
└────────────────────────┬─────────────────────────────────────┘
                         │ 使用
          ┌──────────────┼──────────────┐
          │              │              │
┌─────────▼──────┐ ┌────▼────────┐ ┌───▼──────────────┐
│ DayNightConfig │ │ IDayNight   │ │ DayNightContext   │
│ SO (数据层)    │ │ Effect      │ │ (值类型上下文)    │
│                │ │ (接口)      │ │                   │
│ - 强度曲线     │ │ Initialize  │ │ - CurrentHour     │
│ - 颜色渐变     │ │ Tick        │ │ - NormalizedHour  │
│ - 太阳角度     │ │ IsActive    │ │ - NightLevel      │
│ - 夜晚程度     │ │             │ │ - CurrentPhase    │
└────────────────┘ └─────────────┘ │ - CurrentSeason   │
                                   │ - DeltaTime       │
                                   └───────────────────┘
```

### 3.2 文件结构

```
Assets/
  Scripts/
    LightingSystem/
      DayNightManager.cs          ← 核心管理器（单例）
      DayNightConfigSO.cs         ← 光照配置数据（ScriptableObject）
      IDayNightEffect.cs          ← 效果插件接口 + DayNightContext 结构体
      Effects/                    ← 未来效果插件目录（预留）
        (NightSanityEffect.cs)    ← 未来: 夜晚理智值效果
        (ScreenVignetteEffect.cs) ← 未来: 屏幕暗角效果

  SO/
    LightingData/
      DefaultDayNightConfig.asset ← 默认（春季）光照配置
      (SummerConfig.asset)        ← 未来: 夏季配置
      (AutumnConfig.asset)        ← 未来: 秋季配置
      (WinterConfig.asset)        ← 未来: 冬季配置
```

### 3.3 与现有系统的关系

```
                 ┌──────────────┐
                 │  TimeManager │ (只读依赖)
                 └──────┬───────┘
                        │
          ┌─────────────┼─────────────┐
          │             │             │
   ┌──────▼──────┐ ┌───▼───────┐ ┌──▼────────────┐
   │ DayNight    │ │ ClockUI   │ │ CropBase      │
   │ Manager     │ │ (现有)    │ │ (现有)        │
   │ (新)        │ │ 轮询时间  │ │ 订阅OnDay事件 │
   └─────────────┘ └───────────┘ └───────────────┘
```

**关键原则**：DayNightManager 对 TimeManager 是**单向只读依赖**，不修改 TimeManager 的任何代码。

---

## 4. 详细功能设计

### 4.1 DayNightConfigSO - 光照配置数据

```csharp
using UnityEngine;
using TimeSystem;

namespace LightingSystem
{
    /// <summary>
    /// 日夜光照配置数据。
    /// 所有光照参数通过 AnimationCurve 和 Gradient 定义，
    /// X轴统一使用归一化时间 (0 = 0:00, 1 = 24:00)。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDayNightConfig", menuName = "Lighting/Day Night Config")]
    public class DayNightConfigSO : ScriptableObject
    {
        [Header("目标季节")]
        [Tooltip("此配置适用的季节。默认配置设置为 Spring 即可。")]
        public Season targetSeason = Season.Spring;

        [Header("平行光 - 强度")]
        [Tooltip("X轴: 归一化时间 (0=0:00, 1=24:00). Y轴: 灯光强度 (0~1.5).")]
        public AnimationCurve lightIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.05f),       // 0:00  深夜
            new Keyframe(0.21f, 0.1f),     // 5:00  黎明
            new Keyframe(0.29f, 0.7f),     // 7:00  早晨
            new Keyframe(0.5f, 1.0f),      // 12:00 正午
            new Keyframe(0.71f, 0.6f),     // 17:00 傍晚
            new Keyframe(0.83f, 0.1f),     // 20:00 入夜
            new Keyframe(1f, 0.05f)        // 24:00 深夜
        );

        [Header("平行光 - 颜色")]
        [Tooltip("X轴: 归一化时间. 控制平行光颜色随时间变化.")]
        public Gradient lightColorGradient;

        [Header("平行光 - 太阳仰角 (X轴旋转)")]
        [Tooltip("X轴: 归一化时间. Y轴: 旋转角度(度). 正值=从上方照射, 负值=地平线以下.")]
        public AnimationCurve sunPitchCurve = new AnimationCurve(
            new Keyframe(0f, -30f),        // 0:00  地平线以下
            new Keyframe(0.21f, 5f),       // 5:00  刚升起
            new Keyframe(0.29f, 30f),      // 7:00  低角度（长影子）
            new Keyframe(0.5f, 75f),       // 12:00 接近头顶（短影子）
            new Keyframe(0.71f, 20f),      // 17:00 低角度（长影子）
            new Keyframe(0.83f, -10f),     // 20:00 落下
            new Keyframe(1f, -30f)         // 24:00 地平线以下
        );

        [Header("平行光 - 水平方向 (Y轴旋转)")]
        [Tooltip("太阳的水平方向角度. 通常固定为一个值, 如 -30 度.")]
        public AnimationCurve sunYawCurve = AnimationCurve.Constant(0f, 1f, -30f);

        [Header("环境光 - 颜色")]
        [Tooltip("X轴: 归一化时间. 控制 RenderSettings.ambientLight 颜色.")]
        public Gradient ambientColorGradient;

        [Header("环境光 - 强度")]
        [Tooltip("X轴: 归一化时间. Y轴: RenderSettings.ambientIntensity (0~1).")]
        public AnimationCurve ambientIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.08f),       // 深夜
            new Keyframe(0.21f, 0.15f),    // 黎明
            new Keyframe(0.29f, 0.5f),     // 早晨
            new Keyframe(0.5f, 0.8f),      // 正午
            new Keyframe(0.71f, 0.4f),     // 傍晚
            new Keyframe(0.83f, 0.1f),     // 入夜
            new Keyframe(1f, 0.08f)        // 深夜
        );

        [Header("夜晚程度曲线 (供效果插件使用)")]
        [Tooltip("X轴: 归一化时间. Y轴: 0=完全白天, 1=完全深夜. 用于 IDayNightEffect 读取.")]
        public AnimationCurve nightLevelCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f),        // 0:00  深夜
            new Keyframe(0.21f, 0.7f),     // 5:00  黎明
            new Keyframe(0.29f, 0.1f),     // 7:00  进入白天
            new Keyframe(0.5f, 0.0f),      // 12:00 正午
            new Keyframe(0.71f, 0.2f),     // 17:00 傍晚
            new Keyframe(0.83f, 0.9f),     // 20:00 入夜
            new Keyframe(1f, 1.0f)         // 24:00 深夜
        );

        private void OnValidate()
        {
            // 确保强度曲线值不为负
            ClampCurveMinimum(lightIntensityCurve, 0f);
            ClampCurveMinimum(ambientIntensityCurve, 0f);

            // 确保夜晚程度在 0~1 范围
            ClampCurveRange(nightLevelCurve, 0f, 1f);
        }

        private void ClampCurveMinimum(AnimationCurve curve, float min)
        {
            if (curve == null) return;
            for (int i = 0; i < curve.length; i++)
            {
                var key = curve[i];
                if (key.value < min)
                {
                    key.value = min;
                    curve.MoveKey(i, key);
                }
            }
        }

        private void ClampCurveRange(AnimationCurve curve, float min, float max)
        {
            if (curve == null) return;
            for (int i = 0; i < curve.length; i++)
            {
                var key = curve[i];
                if (key.value < min || key.value > max)
                {
                    key.value = Mathf.Clamp(key.value, min, max);
                    curve.MoveKey(i, key);
                }
            }
        }
    }
}
```

### 4.2 IDayNightEffect - 效果插件接口

```csharp
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

        /// <summary>是否处于夜晚时段 (快捷属性)</summary>
        public bool IsNight;

        /// <summary>本帧 deltaTime</summary>
        public float DeltaTime;
    }

    /// <summary>
    /// 日夜效果插件接口。
    /// 实现此接口的 MonoBehaviour 添加到 DayNightManager 的效果列表后，
    /// 每帧会收到 DayNightContext 并执行自定义逻辑。
    ///
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
```

### 4.3 DayNightManager - 核心管理器

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("效果插件")]
        [Tooltip("实现 IDayNightEffect 接口的 MonoBehaviour（拖入场景对象）")]
        [SerializeField] private List<MonoBehaviour> effectBehaviours;
        #endregion

        #region Private Fields
        private TimeManager timeManager;
        private DayNightConfigSO activeConfig;
        private List<IDayNightEffect> effects = new List<IDayNightEffect>();
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

            // 获取 TimeManager 引用
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

            // 强制设置环境光模式为 Flat，确保 ambientLight 可写
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            // 订阅季节变化事件
            timeManager.OnSeasonChanged += OnSeasonChanged;

            // 立即同步初始光照状态
            ApplyLighting(timeManager.CurrentHour);

            // 初始化所有效果插件
            DayNightContext initialCtx = BuildContext(timeManager.CurrentHour);
            foreach (var effect in effects)
            {
                effect.Initialize(initialCtx);
            }

            Debug.Log($"[DayNightManager] 初始化完成 - 配置: {activeConfig.name}");
        }

        private void Update()
        {
            if (timeManager == null || activeConfig == null) return;

            float hour = timeManager.CurrentHour;

            // 每帧平滑更新光照（同 ClockUI 的轮询模式）
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
```

---

## 5. 光照参数配置表

### 5.1 默认配置（春季）推荐值

以下为 `DefaultDayNightConfig` 的推荐关键帧值：

#### 平行光强度曲线 (lightIntensityCurve)

| 时间 | 归一化值 | 强度 | 说明 |
|------|----------|------|------|
| 0:00 | 0.00 | 0.05 | 深夜，月光微弱 |
| 5:00 | 0.21 | 0.10 | 黎明初现 |
| 7:00 | 0.29 | 0.70 | 早晨，快速变亮 |
| 12:00 | 0.50 | 1.00 | 正午，最亮 |
| 17:00 | 0.71 | 0.60 | 傍晚，开始变暗 |
| 20:00 | 0.83 | 0.10 | 入夜 |
| 24:00 | 1.00 | 0.05 | 深夜 |

```
1.0 |              ╱ ─ ─ ╲
    |            ╱         ╲
0.7 |          ╱             ╲
    |        ╱                 ╲
0.1 |──╲──╱                     ╲──
0.05|    ╲                         ╲
    ├────┬────┬────┬────┬────┬────┤
   0:00 4:00 8:00 12:00 16:00 20:00 24:00
```

#### 平行光颜色渐变 (lightColorGradient)

| 归一化值 | 颜色 | 说明 |
|----------|------|------|
| 0.00 | #1A1A40 (深蓝) | 月光 |
| 0.20 | #2B2B5E (暗蓝) | 深夜→黎明过渡 |
| 0.23 | #FF8C42 (橘黄) | 日出 |
| 0.30 | #FFE4B5 (暖白) | 早晨 |
| 0.50 | #FFFFF0 (纯白微暖) | 正午 |
| 0.70 | #FFE4B5 (暖白) | 下午 |
| 0.80 | #FF6B35 (橘红) | 日落 |
| 0.85 | #2B2B5E (暗蓝) | 入夜 |
| 1.00 | #1A1A40 (深蓝) | 月光 |

#### 环境光颜色渐变 (ambientColorGradient)

| 归一化值 | 颜色 | 说明 |
|----------|------|------|
| 0.00 | #0A0A20 (极暗蓝) | 深夜阴影 |
| 0.25 | #3A2820 (暗棕暖) | 黎明阴影 |
| 0.50 | #8090A0 (浅灰蓝) | 正午阴影 |
| 0.75 | #4A3020 (暖棕) | 傍晚阴影 |
| 1.00 | #0A0A20 (极暗蓝) | 深夜阴影 |

#### 太阳仰角曲线 (sunPitchCurve)

| 时间 | 角度 | 说明 |
|------|------|------|
| 0:00 | -30° | 地平线以下 |
| 5:00 | 5° | 日出，刚出地平线 |
| 7:00 | 30° | 低角度，长影子 |
| 12:00 | 75° | 接近头顶，短影子 |
| 17:00 | 20° | 低角度，长影子 |
| 20:00 | -10° | 日落 |
| 24:00 | -30° | 地平线以下 |

#### 夜晚程度曲线 (nightLevelCurve)

| 时间 | 夜晚程度 | 说明 |
|------|----------|------|
| 0:00 | 1.0 | 深夜 |
| 5:00 | 0.7 | 黎明 |
| 7:00 | 0.1 | 进入白天 |
| 12:00 | 0.0 | 正午 |
| 17:00 | 0.2 | 傍晚 |
| 20:00 | 0.9 | 入夜 |
| 24:00 | 1.0 | 深夜 |

---

## 6. 季节差异化方案

### 6.1 实现方式

每个季节创建独立的 `DayNightConfigSO` 资产，设置不同的曲线和颜色。将它们添加到 `DayNightManager.seasonConfigs` 列表中，当 `TimeManager.OnSeasonChanged` 触发时自动切换。

### 6.2 季节光照特征

| 季节 | 正午最大强度 | 色调特征 | 日出/日落时间感 | 备注 |
|------|-------------|----------|----------------|------|
| 春季 | 1.0 | 暖白，偏黄绿 | 正常 | 默认配置 |
| 夏季 | 1.2 | 强烈暖白，偏黄 | 日照时间长 | 曲线两端更窄 |
| 秋季 | 0.9 | 暖橙色调 | 日照时间略短 | 傍晚橘色更浓 |
| 冬季 | 0.7 | 冷白，偏蓝灰 | 日照时间短 | 曲线两端更宽 |

### 6.3 工作流程

```
1. 在 Project 窗口: 右键 → Create → Lighting → Day Night Config
2. 设置 targetSeason = 对应季节
3. 调整曲线和渐变色
4. 拖入 DayNightManager 的 seasonConfigs 列表
5. 运行游戏，等待季节变化验证（或用 SkipToNextDay 快进）
```

不需要修改任何代码。

---

## 7. 可扩展性设计

### 7.1 效果插件模式

`IDayNightEffect` 接口是系统的扩展点。任何 MonoBehaviour 实现该接口后，添加到 `DayNightManager.effectBehaviours` 列表即可自动接入。

**DayNightManager 对具体效果完全解耦**——它只知道接口，不知道实现。

### 7.2 可接入的未来效果示例

| 效果 | 使用的上下文字段 | 简述 |
|------|-----------------|------|
| 夜晚理智值消耗 | `NightLevel`, `IsNight`, `DeltaTime` | 夜间持续降低理智值，白天恢复 |
| 屏幕暗角 (Vignette) | `NightLevel` | 夜晚越深暗角越重，可结合理智值加重 |
| 萤火虫粒子 | `IsNight`, `NightLevel` | 夜晚自动生成萤火虫粒子系统 |
| 夜行怪物生成 | `IsNight`, `CurrentPhase` | Night 时段触发怪物生成逻辑 |
| 篝火光照效果 | `NightLevel` | 夜间篝火点亮，加强周围光照 |

### 7.3 动态注册

除了 Inspector 拖拽外，效果也可在运行时通过代码注册：

```csharp
// 注册
DayNightManager.Instance.RegisterEffect(myEffect);

// 移除
DayNightManager.Instance.UnregisterEffect(myEffect);
```

### 7.4 外部系统读取夜晚程度

其他系统（如未来的理智值、怪物AI）可以直接读取：

```csharp
float nightLevel = DayNightManager.Instance.CurrentNightLevel;
```

或订阅事件：

```csharp
DayNightManager.Instance.OnNightLevelChanged += (level) => {
    // 根据 nightLevel 执行逻辑
};
```

---

## 8. 与TimeManager集成

### 8.1 集成方式总结

| 集成点 | 方式 | 用途 |
|--------|------|------|
| `CurrentHour` | Update() 逐帧轮询 | 平滑光照插值 |
| `CurrentPhase` | 读取属性 | 填充 DayNightContext.IsNight |
| `CurrentSeason` | 读取属性 | 填充 DayNightContext |
| `OnSeasonChanged` | 事件订阅 | 切换季节光照配置 |

### 8.2 不修改 TimeManager

DayNightManager 完全是**单向只读依赖**，TimeManager 不需要知道光照系统的存在。

---

## 9. Unity Editor配置步骤

### 步骤1: 修改 Directional Light 模式

1. 在 Hierarchy 中选择 `Directional Light`
2. Inspector → Light 组件 → **Mode**: 从 `Baked` 改为 `Realtime`

> 原因: Baked 模式下灯光参数在运行时不可变，必须改为 Realtime 才能被脚本动态控制。

### 步骤2: 修改环境光模式

1. 菜单: Window → Rendering → Lighting
2. Environment 标签 → **Source**: 从 `Skybox` 改为 `Color`

> 原因: Skybox 模式下 `RenderSettings.ambientLight` 写入被忽略。脚本也会在初始化时强制设置，但手动改一次更稳妥。

### 步骤3: 创建光照配置 SO

1. Project 窗口 → 在 `Assets/SO/LightingData/` 目录下
2. 右键 → Create → **Lighting → Day Night Config**
3. 命名为 `DefaultDayNightConfig`
4. 在 Inspector 中配置曲线和渐变色（参考第5节的配置表）

### 步骤4: 创建 DayNightManager 对象

1. Hierarchy → Create Empty → 命名 `DayNightManager`
2. 添加组件 → `DayNightManager` 脚本
3. 拖入引用:
   - **Directional Light**: 拖入场景中的平行光
   - **Default Config**: 拖入 `DefaultDayNightConfig` SO 资产
   - **Season Configs**: 留空（可选，后续添加）
   - **Effect Behaviours**: 留空（可选，后续添加效果）

### 步骤5: 脚本执行顺序（推荐但非必须）

1. Edit → Project Settings → Script Execution Order
2. 设置 `TimeManager` = -100
3. 设置 `DayNightManager` = -50

> 系统已通过 DelayedInit 协程处理了初始化顺序，但显式设置更稳妥。

---

## 10. 开发路线图

### 阶段1: 核心光照系统

- [ ] 创建 `DayNightConfigSO.cs`
- [ ] 创建 `IDayNightEffect.cs` + `DayNightContext` 结构体
- [ ] 创建 `DayNightManager.cs`
- [ ] 修改 Directional Light 为 Realtime 模式
- [ ] 创建 DefaultDayNightConfig SO 资产并配置曲线
- [ ] 场景中创建 DayNightManager 对象并挂载
- [ ] 验证: 运行游戏，观察光照随时间平滑变化

### 阶段2: 配置调优

- [ ] 微调强度曲线，确保日出/日落过渡自然
- [ ] 微调颜色渐变，确保氛围感到位
- [ ] 调整太阳角度曲线，确保影子变化合理
- [ ] 用 `SetTimeScale` 加速验证全天光照循环

### 阶段3: 季节差异化（可选）

- [ ] 创建夏/秋/冬三个 DayNightConfigSO 资产
- [ ] 添加到 DayNightManager.seasonConfigs 列表
- [ ] 验证季节切换时光照配置自动切换

### 阶段4: 效果插件扩展（未来）

- [ ] 夜晚理智值消耗效果
- [ ] 屏幕暗角效果
- [ ] 其他夜间效果...

---

## 附录

### A. 注意事项

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| `ambientLight` 写入无效 | AmbientMode 不是 Flat | 脚本初始化时强制设置 `AmbientMode.Flat` |
| 太阳角度在午夜前后跳变 | AnimationCurve 关键帧设置不当 | 确保曲线连续，使用 Clamped Auto 切线 |
| Inspector 拖入的效果组件报警告 | 组件未实现 IDayNightEffect | CollectEffects 会打印警告并跳过 |
| Gradient 字段为 null | SO 资产中未手动配置渐变色 | 务必在 Inspector 中配置，至少设置两个颜色节点 |

### B. 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2026-02-27 | 初始策划文档完成 |

---

**文档结束**
