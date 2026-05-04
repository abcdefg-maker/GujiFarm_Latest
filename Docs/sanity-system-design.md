# SAN值（理智值）系统 - 策划文档

> **项目**: GuJi_Farm_Project
> **Unity 版本**: 2022.3.62f3c1 (LTS)
> **日期**: 2026-03-07
> **版本**: v1.0
> **依赖**: TimeSystem (TimeManager), LightingSystem (DayNightManager, IDayNightEffect), FarmingSystem (CropData, SoilMound)

---

## 目录

1. [系统概述](#1-系统概述)
2. [核心机制设计](#2-核心机制设计)
3. [系统架构设计](#3-系统架构设计)
4. [详细功能设计](#4-详细功能设计)
5. [变异作物系统](#5-变异作物系统)
6. [疯狂症状系统](#6-疯狂症状系统)
7. [SAN消耗来源](#7-san消耗来源)
8. [游戏结束流程](#8-游戏结束流程)
9. [与现有系统集成](#9-与现有系统集成)
10. [Unity Editor配置步骤](#10-unity-editor配置步骤)
11. [开发路线图](#11-开发路线图)

---

## 1. 系统概述

### 1.1 设计目标

为主角引入 **SAN值（理智值/Sanity）** 系统，作为游戏的核心生存压力机制。SAN值的下降会带来以下后果：

- **疯狂症状**：每降低 10 点触发一次疯狂症状效果
- **作物变异**：低SAN时种植的作物有概率变异（如番茄→变异番茄）
- **游戏结束**：SAN归零时主角永久疯狂，游戏结束

灵感来源：《克苏鲁的呼唤》的理智值 + 《饥荒》的精神值机制。

### 1.2 核心特性

- **数值系统**：SAN值范围 0~100，初始为 100
- **持续消耗**：夜晚时自动消耗SAN值（利用现有 `IDayNightEffect` 接口）
- **阈值触发**：每跨越 10 的倍数（90、80、70...）触发疯狂症状
- **作物变异**：种植时根据当前SAN值概率性替换为变异作物
- **游戏结束**：SAN归零 → 永久疯狂 → 游戏结束（后续改为回档到当天早晨）
- **可扩展**：通过接口模式支持未来新增消耗来源和症状效果

### 1.3 设计约束

| 约束项 | 现状 | 说明 |
|--------|------|------|
| 玩家属性系统 | 不存在 | Player.cs 只处理输入和移动，无任何属性 |
| 存档系统 | 不存在 | 游戏结束暂用结束画面，后续改为回档 |
| SAN恢复 | 暂不实现 | 当前阶段SAN只降不升，后续补充恢复途径 |
| 疯狂症状 | 仅接口 | 当前只定义接口，具体症状效果后续补全 |

---

## 2. 核心机制设计

### 2.1 SAN值参数

| 参数 | 值 | 说明 |
|------|-----|------|
| 最大值 | 100 | 理智值上限 |
| 初始值 | 100 | 游戏开始时满SAN |
| 游戏结束阈值 | 0 | SAN归零触发游戏结束 |
| 症状触发间隔 | 10 | 每降10点触发一次症状 |
| 变异起始阈值 | 80 | SAN低于80才可能出现变异 |

### 2.2 SAN消耗方式

SAN消耗分为两种模式：

| 模式 | 机制 | 示例 |
|------|------|------|
| **持续消耗** | 实现 `ISanityDrainSource` 接口，每帧计算消耗量 | 夜晚自动消耗、靠近怪物 |
| **一次性消耗** | 直接调用 `SanityManager.DrainSanity(amount)` | 遭遇事件、使用诅咒物品 |

当前阶段只实现**夜晚持续消耗**作为示例。

### 2.3 变异概率计算

变异概率由 AnimationCurve 控制，设计师可在 Inspector 中自由调节曲线形状：

```
变异概率
0.8 |╲
    |  ╲
0.5 |    ╲
    |      ╲
0.2 |        ╲
    |          ╲
0.05|            ╲
0.0 |──────────────╲────
    0   20   40   60   80   100  ← SAN值
```

| SAN值 | 变异概率 | 说明 |
|-------|----------|------|
| 100~80 | 0% | 正常，不会变异 |
| 80 | 5% | 刚开始可能变异 |
| 50 | 20% | 中等概率 |
| 20 | 60% | 高概率 |
| 0 | 80% | 极高概率（保留20%正常） |

每种作物还可以有独立的**概率倍率**（如某些作物更容易变异），通过 `MutationTable` 配置。

最终变异概率 = `曲线概率 × 品种倍率`，上限为 1.0。

### 2.4 阈值触发逻辑

```
SAN从85降到65的情况：

100 ─────────────────
 90 ─ ─ ─ ─ ─ ─ ─ ─   ← 已跨越
 80 ─ ─ ─ ─ ─ ─ ─ ─   ← 触发症状！（跨越80）
 70 ─ ─ ─ ─ ─ ─ ─ ─   ← 触发症状！（跨越70）
 65 ▪ (当前SAN)
 60 ─ ─ ─ ─ ─ ─ ─ ─

结果：一次SAN下降可能触发多个阈值的症状
```

---

## 3. 系统架构设计

### 3.1 文件结构

```
Assets/Scripts/
  SanitySystem/
    SanityConstants.cs        ← 系统常量（仿 GameConstants.cs 模式）
    ISanityDrainSource.cs     ← 消耗来源接口
    IMadnessSymptom.cs        ← 疯狂症状接口（预留）
    SanityConfigSO.cs         ← 配置数据 ScriptableObject
    MutationTable.cs          ← 变异映射表 ScriptableObject
    SanityManager.cs          ← 核心管理器（单例）
    NightSanityDrain.cs       ← 夜晚消耗（ISanityDrainSource + IDayNightEffect）
    SanityUI.cs               ← HUD 理智值显示

Assets/SO/
  SanityData/
    DefaultSanityConfig.asset ← 默认SAN配置
    MutationTable.asset       ← 变异映射表

  FarmingData/
    MutatedTomato.asset       ← （示例）变异番茄 CropData
    ...                       ← 其他变异作物
```

### 3.2 系统架构图

```
┌──────────────────────────────────────────────────────────┐
│                  TimeManager (现有，不修改)                │
│  提供: CurrentHour, CurrentPhase                          │
│  事件: OnDayChanged                                      │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│              DayNightManager (现有，不修改)                │
│  提供: DayNightContext (NightLevel, IsNight 等)           │
│  接口: IDayNightEffect (Initialize / Tick / IsActive)    │
└────────────────────────┬─────────────────────────────────┘
                         │ IDayNightEffect.Tick()
┌────────────────────────▼─────────────────────────────────┐
│              NightSanityDrain (新建)                      │
│  实现: ISanityDrainSource + IDayNightEffect               │
│  职责: 根据 NightLevel 计算每帧SAN消耗量                  │
└────────────────────────┬─────────────────────────────────┘
                         │ ISanityDrainSource.GetDrainAmount()
┌────────────────────────▼─────────────────────────────────┐
│              SanityManager (新建，核心单例)                │
│  ┌──────────────────────────────────────────────────┐    │
│  │  职责:                                            │    │
│  │  1. 管理SAN值（唯一修改入口: ModifySanity）       │    │
│  │  2. 每帧处理所有 ISanityDrainSource              │    │
│  │  3. 检测阈值跨越，触发 IMadnessSymptom           │    │
│  │  4. 评估种植变异（EvaluateMutation）              │    │
│  │  5. 触发游戏结束                                  │    │
│  └──────────────────────────────────────────────────┘    │
│                                                          │
│  持有: SanityConfigSO (曲线/参数配置)                    │
│  持有: MutationTable (变异映射数据)                      │
│  持有: List<IMadnessSymptom> (症状列表)                  │
│  广播: OnSanityChanged, OnMadnessThresholdCrossed,       │
│        OnCropMutated, OnGameOver                          │
└──────────┬──────────────────────────┬────────────────────┘
           │                          │
┌──────────▼──────────┐   ┌──────────▼──────────┐
│  SanityUI (新建)    │   │  SoilMound (修改)    │
│  HUD 显示SAN值      │   │  TryPlant中调用      │
│  游戏结束面板       │   │  EvaluateMutation    │
└─────────────────────┘   └─────────────────────┘
```

### 3.3 与现有系统的关系

```
                     ┌──────────────┐
                     │  TimeManager │ (只读依赖，不修改)
                     └──────┬───────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
┌─────────▼──────┐ ┌───────▼───────┐ ┌───────▼───────┐
│ DayNightManager│ │ CropBase      │ │ SanityManager │
│ (现有)         │ │ (现有)        │ │ (新建)        │
│ 提供NightLevel │ │ 订阅OnDay事件 │ │ 管理SAN值     │
└──────┬─────────┘ └───────────────┘ └───────┬───────┘
       │                                      │
       │ IDayNightEffect                      │ EvaluateMutation
       │                                      │
┌──────▼───────────┐               ┌──────────▼──────┐
│ NightSanityDrain │               │ SoilMound       │
│ (新建)           │──────────────→│ (修改TryPlant)  │
│ ISanityDrainSource│  注册消耗源   └─────────────────┘
└──────────────────┘
```

**关键原则**：
- SanityManager 对 TimeManager、DayNightManager 是**单向只读依赖**
- 对 FarmingSystem 是**最小侵入**：仅修改 SoilMound.TryPlant 的3行代码
- CropData 添加的字段是**非破坏性**的（默认值不影响现有数据）

---

## 4. 详细功能设计

### 4.1 SanityConstants.cs - 系统常量

```csharp
namespace SanitySystem
{
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
```

### 4.2 ISanityDrainSource.cs - 消耗来源接口

```csharp
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
```

### 4.3 IMadnessSymptom.cs - 疯狂症状接口

```csharp
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

        /// <summary>优先级（多个症状同时触发时的执行顺序）</summary>
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
```

### 4.4 SanityConfigSO.cs - 配置 ScriptableObject

```csharp
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
    }
}
```

### 4.5 MutationTable.cs - 变异映射表

```csharp
using System;
using UnityEngine;
using FarmingSystem;

namespace SanitySystem
{
    /// <summary>
    /// 变异映射表 - 数据驱动。
    /// 定义正常作物到变异作物的映射关系。
    /// 设计师在 Inspector 中配置，无需修改代码即可新增变异品种。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMutationTable", menuName = "Sanity/Mutation Table")]
    public class MutationTable : ScriptableObject
    {
        [Serializable]
        public struct MutationEntry
        {
            [Tooltip("正常作物数据")]
            public CropData normalCrop;

            [Tooltip("变异版本的作物数据")]
            public CropData mutatedCrop;

            [Tooltip("该品种的变异概率倍率（1.0=使用默认曲线）")]
            [Range(0.1f, 3f)]
            public float probabilityMultiplier;
        }

        [Header("变异映射列表")]
        public MutationEntry[] entries;

        /// <summary>查找变异版本，无则返回null</summary>
        public CropData GetMutatedVariant(CropData normalCrop) { ... }

        /// <summary>获取品种概率倍率</summary>
        public float GetProbabilityMultiplier(CropData normalCrop) { ... }

        /// <summary>是否有变异版本</summary>
        public bool HasMutatedVariant(CropData normalCrop) { ... }
    }
}
```

### 4.6 SanityManager.cs - 核心管理器

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FarmingSystem;

namespace SanitySystem
{
    /// <summary>
    /// SAN（理智值）管理器 - 单例。
    /// 管理SAN值、处理消耗来源、触发疯狂症状、评估作物变异、处理游戏结束。
    /// </summary>
    public class SanityManager : MonoBehaviour
    {
        #region Singleton
        public static SanityManager Instance { get; private set; }
        #endregion

        #region Inspector字段
        [Header("配置")]
        [SerializeField] private SanityConfigSO config;

        [Header("变异")]
        [SerializeField] private MutationTable mutationTable;

        [Header("疯狂症状")]
        [SerializeField] private List<MonoBehaviour> symptomBehaviours;
        #endregion

        #region 公共属性
        public float CurrentSanity => currentSanity;
        public float MaxSanity => config.maxSanity;
        public float NormalizedSanity => currentSanity / MaxSanity;  // 0~1
        public bool IsGameOver => isGameOver;
        #endregion

        #region 事件
        /// <summary>SAN值变化时触发。参数: 新SAN值</summary>
        public event Action<float> OnSanityChanged;

        /// <summary>跨越疯狂阈值时触发。参数: 跨越的阈值(90/80/70...)</summary>
        public event Action<int> OnMadnessThresholdCrossed;

        /// <summary>作物变异发生时触发。参数: (原始CropData, 变异CropData)</summary>
        public event Action<CropData, CropData> OnCropMutated;

        /// <summary>游戏结束时触发（SAN归零）</summary>
        public event Action OnGameOver;
        #endregion

        #region 核心方法

        /// <summary>
        /// 修改SAN值 - 所有SAN变化的唯一入口。
        /// 正值=恢复，负值=消耗。
        /// </summary>
        public void ModifySanity(float delta) { ... }

        /// <summary>便捷方法: 消耗SAN（传入正值）</summary>
        public void DrainSanity(float amount) { ... }

        /// <summary>便捷方法: 恢复SAN（传入正值，当前阶段预留）</summary>
        public void RestoreSanity(float amount) { ... }

        /// <summary>
        /// 评估作物变异 - 种植时调用。
        /// 根据当前SAN值和变异表决定是否替换为变异作物。
        /// 返回实际使用的CropData（原始或变异版本）。
        /// </summary>
        public CropData EvaluateMutation(CropData originalCrop) { ... }

        /// <summary>注册持续消耗来源</summary>
        public void RegisterDrainSource(ISanityDrainSource source) { ... }

        /// <summary>注销持续消耗来源</summary>
        public void UnregisterDrainSource(ISanityDrainSource source) { ... }

        /// <summary>注册疯狂症状</summary>
        public void RegisterSymptom(IMadnessSymptom symptom) { ... }

        /// <summary>注销疯狂症状</summary>
        public void UnregisterSymptom(IMadnessSymptom symptom) { ... }

        #endregion
    }
}
```

**Update循环逻辑**:

```
每帧:
  if (isGameOver) return;

  遍历所有注册的 ISanityDrainSource:
    if (source.IsActive):
      totalDrain += source.GetDrainAmount(deltaTime, currentSanity)

  if (totalDrain != 0):
    ModifySanity(-totalDrain)
```

**阈值检测逻辑**:

```
ModifySanity(delta):
  previousSanity = currentSanity
  currentSanity = Clamp(currentSanity + delta, 0, maxSanity)

  OnSanityChanged?.Invoke(currentSanity)

  if delta < 0:  // 仅在SAN下降时检测
    prevBucket = Floor(previousSanity / interval)  // interval=10
    newBucket = Floor(currentSanity / interval)

    for bucket = prevBucket downto newBucket+1:
      thresholdValue = bucket × interval
      OnMadnessThresholdCrossed?.Invoke(thresholdValue)
      触发匹配的 IMadnessSymptom

  if currentSanity <= 0:
    TriggerGameOver()
```

### 4.7 NightSanityDrain.cs - 夜晚消耗

```csharp
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
        // ISanityDrainSource
        public string SourceID => "NightDrain";
        public bool IsActive => currentNightLevel > nightLevelThreshold;

        // IDayNightEffect
        bool IDayNightEffect.IsActive => true;

        private float currentNightLevel = 0f;
        private float nightLevelThreshold = 0.5f;

        // Start(): 注册到 SanityManager 和 DayNightManager
        // OnDestroy(): 注销

        // IDayNightEffect.Tick(context): 更新 currentNightLevel
        // ISanityDrainSource.GetDrainAmount(): 返回 config.GetNightDrainRate(nightLevel) × deltaTime
    }
}
```

### 4.8 SanityUI.cs - HUD显示

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SanitySystem
{
    /// <summary>
    /// SAN值 HUD 显示。
    /// 遵循 CurrencyUI 的 DelayedInit + 事件订阅模式。
    /// </summary>
    public class SanityUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private TextMeshProUGUI sanityText;    // "85 / 100"
        [SerializeField] private Image sanityFillBar;           // 进度条（可选）

        [Header("视觉反馈")]
        [SerializeField] private Gradient barColorGradient;     // 低SAN=红, 高SAN=绿

        [Header("游戏结束")]
        [SerializeField] private GameObject gameOverPanel;      // 游戏结束面板

        // 订阅 OnSanityChanged → 刷新数值和进度条
        // 订阅 OnGameOver → 显示游戏结束面板
    }
}
```

---

## 5. 变异作物系统

### 5.1 变异时机

变异在**种植时**决定，而非收获时。

优势：
- 玩家种下种子后立即看到变异作物的外观（变异生长精灵）
- 提供即时视觉反馈，增强SAN值下降的恐惧感
- 技术实现简洁，只需修改种植流程的一个点

### 5.2 CropData.cs 修改

在现有 CropData 末尾添加变异标记字段：

```csharp
[Header("变异配置")]
[Tooltip("是否为变异作物")]
public bool isMutated = false;

[Tooltip("如果是变异作物，引用其正常版本")]
public CropData normalVariant;
```

**非破坏性修改**：所有现有 CropData 资产的 `isMutated` 默认为 `false`，不影响现有数据。

### 5.3 SoilMound.cs 修改

在 `TryPlant` 方法中插入变异检查（3行代码）：

```csharp
public bool TryPlant(CropData cropData, GameObject planter)
{
    // ... 现有的空状态检查 ...
    // ... 现有的季节检查 ...

    // ====== 新增: 变异检查 ======
    CropData actualCropData = cropData;
    if (SanityManager.Instance != null)
    {
        actualCropData = SanityManager.Instance.EvaluateMutation(cropData);
    }
    // ============================

    CreateCrop(actualCropData);  // 原来是 CreateCrop(cropData)

    // ... 后续逻辑不变 ...
}
```

如果 SanityManager 不存在（如测试场景），安全降级为使用原始 cropData。

### 5.4 变异作物 CropData 配置示例

| 字段 | 正常番茄 | 变异番茄 |
|------|----------|----------|
| cropID | crop_tomato | crop_mutated_tomato |
| cropName | 番茄 | 变异番茄 |
| isMutated | false | true |
| normalVariant | null | → 番茄 CropData |
| stageSprites | 正常外观 | 诡异/发光外观 |
| harvestItemPrefab | 番茄 Item | 变异番茄 Item |
| sellPrice | (正常价格) | (可能更高或更低) |

### 5.5 MutationTable 配置示例

| 正常作物 | 变异版本 | 概率倍率 | 说明 |
|----------|----------|----------|------|
| 番茄 | 变异番茄 | 1.0 | 标准概率 |
| 胡萝卜 | 变异胡萝卜 | 1.2 | 更容易变异 |
| 小麦 | 变异小麦 | 0.8 | 较难变异 |

---

## 6. 疯狂症状系统

### 6.1 当前阶段

当前版本**只定义接口**，不实现具体症状效果。

SanityManager 中的症状触发框架已完整：
- Inspector 拖入实现 `IMadnessSymptom` 的 MonoBehaviour
- 运行时可通过 `RegisterSymptom` / `UnregisterSymptom` 动态管理
- 阈值检测逻辑已实现（支持通配符 `-1` 和特定阈值）

### 6.2 未来症状实现规划

| 阈值 | 症状名称 | 效果描述 | 优先级 |
|------|----------|----------|--------|
| -1 (通配) | 屏幕闪烁 | 每次跨越阈值时屏幕短暂闪烁/扭曲 | 10 |
| 70 | 低语幻听 | 播放诡异低语音效 | 5 |
| 50 | 视觉畸变 | 后处理色调偏移，画面略微扭曲 | 5 |
| 30 | 影子幻觉 | 短暂出现不存在的影子/实体 | 3 |
| 10 | 控制异常 | 移动方向偶尔偏移 | 1 |

### 6.3 实现一个症状只需

```csharp
public class ScreenFlickerSymptom : MonoBehaviour, IMadnessSymptom
{
    public int TriggerThreshold => -1;  // 每个阈值都触发
    public int Priority => 10;
    public bool IsPlaying => isPlaying;

    public void OnTrigger(float prev, float next, int threshold)
    {
        // 播放闪烁效果
    }

    public void OnCancel()
    {
        // 停止闪烁
    }
}
```

然后将其拖入 SanityManager 的 `symptomBehaviours` 列表即可。

---

## 7. SAN消耗来源

### 7.1 当前实现: 夜晚消耗

| 参数 | 默认值 | 说明 |
|------|--------|------|
| nightDrainPerSecond | 0.5 | 每秒基础消耗量 |
| nightLevelThreshold | 0.5 | nightLevel > 0.5 时开始消耗 |
| nightDrainMultiplierCurve | 线性(0→0, 0.5→0.5, 1→1) | 越深夜消耗越快 |

以默认配置为例，完全深夜（nightLevel=1.0）时：
- 每秒消耗 = 0.5 × 1.0 = 0.5 SAN
- 每分钟消耗 ≈ 30 SAN
- 一个完整夜晚（约5分钟游戏内时间）约消耗 ≈ 10~15 SAN

### 7.2 数据流

```
DayNightManager.Update()
  → BuildContext(hour)
  → NightSanityDrain.Tick(context)     // 更新 currentNightLevel

SanityManager.Update()
  → NightSanityDrain.GetDrainAmount()  // 计算消耗量
  → SanityManager.ModifySanity()       // 应用消耗
```

### 7.3 未来消耗来源规划

| 来源 | 类型 | 触发条件 | 消耗量 |
|------|------|----------|--------|
| 夜晚 | 持续 | nightLevel > 0.5 | 0~0.5/秒 |
| 怪物接近 | 持续 | 玩家周围有怪物 | 按距离衰减 |
| 恐怖事件 | 一次性 | 触发特定剧情 | 5~20 |
| 诅咒物品 | 持续 | 背包中有诅咒物品 | 0.1/秒 |
| 饥饿 | 持续 | 饥饿值过低 | 0.2/秒 |

持续消耗实现 `ISanityDrainSource`，一次性消耗直接调用 `DrainSanity()`。

---

## 8. 游戏结束流程

### 8.1 流程图

```
SAN <= 0
  │
  ├→ isGameOver = true（阻止后续SAN变化）
  │
  ├→ OnGameOver 事件触发
  │     ├→ SanityUI: 显示游戏结束面板
  │     └→ 其他系统: 响应游戏结束
  │
  ├→ TimeManager.Pause()（冻结游戏世界）
  │
  └→ 延迟 gameOverDelay 秒后
        └→ 执行游戏结束序列
             └→ [TODO] 存档系统完成后: 回档到当天早晨
```

### 8.2 游戏结束文案

> **"迷失在了无人之境..."**
>
> 你的理智已完全崩溃。
> 主角陷入了永久的疯狂，再也无法返回现实。

### 8.3 后续迭代

存档系统开发完成后，游戏结束流程改为：
1. 显示疯狂画面（同上）
2. 短暂延迟后，自动读取当天早晨的存档
3. SAN值恢复到当天早晨的状态
4. 给玩家"重来一天"的机会

---

## 9. 与现有系统集成

### 9.1 集成点总结

| 现有系统 | 集成方式 | 方向 | 修改量 |
|----------|----------|------|--------|
| TimeManager | NightSanityDrain 通过 DayNightManager 间接获取时间 | 只读 | 无修改 |
| DayNightManager | NightSanityDrain 实现 IDayNightEffect 接口 | 插件注册 | 无修改 |
| FarmingSystem/CropData | 添加 isMutated + normalVariant 字段 | 数据扩展 | 2行 |
| FarmingSystem/SoilMound | TryPlant 中插入变异检查 | 逻辑钩子 | 3行 |
| Player.cs | 无直接修改，SAN不存放在Player上 | 无关 | 无修改 |
| InventorySystem | 未来: 消耗物品恢复SAN | 预留 | 当前无 |
| ShopSystem | 无影响 | 无关 | 无修改 |

### 9.2 不修改的原则

- **TimeManager**: 完全不修改，通过 DayNightManager 间接获取时间上下文
- **DayNightManager**: 完全不修改，通过现有的 `RegisterEffect()` 方法注册效果
- **Player.cs**: 完全不修改，SAN值由独立的 SanityManager 单例管理（与 CurrencyManager 管理金币同理）

---

## 10. Unity Editor配置步骤

### 步骤1: 创建SAN配置 SO

1. Project 窗口 → 在 `Assets/SO/SanityData/` 目录下
2. 右键 → Create → **Sanity → Sanity Config**
3. 命名为 `DefaultSanityConfig`
4. 在 Inspector 中调整变异概率曲线和夜间消耗参数

### 步骤2: 创建变异映射表 SO

1. 同上目录，右键 → Create → **Sanity → Mutation Table**
2. 命名为 `MutationTable`
3. 添加变异映射条目（需先创建变异版本的 CropData）

### 步骤3: 创建变异作物 CropData

1. 在 `Assets/SO/FarmingData/` 目录下
2. 右键 → Create → **Farming → Crop Data**
3. 命名如 `MutatedTomato`
4. 配置变异外观精灵、收获物等
5. 设置 `isMutated = true`，`normalVariant` 指向正常版本

### 步骤4: 创建 SanityManager 对象

1. Hierarchy → Create Empty → 命名 `SanityManager`
2. 添加 `SanityManager` 脚本
3. 拖入引用:
   - **Config**: DefaultSanityConfig SO
   - **Mutation Table**: MutationTable SO
   - **Symptom Behaviours**: 留空（后续添加）

### 步骤5: 创建 NightSanityDrain 对象

1. 可挂载在 SanityManager 同一 GameObject 上，或新建 GameObject
2. 添加 `NightSanityDrain` 脚本
3. 脚本会自动在 Start 中注册到 SanityManager 和 DayNightManager

### 步骤6: 创建 SanityUI

1. 在 HUD Canvas 中创建 SAN 显示元素
2. 添加 `SanityUI` 脚本
3. 拖入 Text/Image 引用
4. 配置颜色渐变（Gradient）
5. 创建游戏结束面板（默认隐藏），拖入 gameOverPanel 引用

---

## 11. 开发路线图

### 阶段1: 核心框架

- [ ] 创建 `SanityConstants.cs`
- [ ] 创建 `ISanityDrainSource.cs`
- [ ] 创建 `IMadnessSymptom.cs`
- [ ] 创建 `SanityConfigSO.cs`
- [ ] 创建 `MutationTable.cs`
- [ ] 创建 `SanityManager.cs`

### 阶段2: 系统集成

- [ ] 修改 `CropData.cs`（添加变异字段）
- [ ] 修改 `SoilMound.cs`（插入变异检查钩子）
- [ ] 创建 `NightSanityDrain.cs`（夜晚消耗实现）

### 阶段3: UI

- [ ] 创建 `SanityUI.cs`（HUD显示）
- [ ] 创建 DefaultSanityConfig SO 资产
- [ ] 创建 MutationTable SO 资产
- [ ] 创建变异作物 CropData SO 资产（至少1个示例）
- [ ] 验证: 夜晚SAN消耗 + 变异种植 + 游戏结束

### 阶段4: 内容填充（未来）

- [ ] 实现具体的 `IMadnessSymptom` 效果类
- [ ] 添加更多 `ISanityDrainSource` 消耗来源
- [ ] SAN恢复途径（食物/睡眠）
- [ ] 接入存档系统后，游戏结束改为回档到当天早晨
- [ ] 变异作物的特殊效果（食用恢复/消耗SAN等）

---

## 附录

### A. 注意事项

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| SanityManager 为 null | 测试场景未放置 SanityManager | SoilMound 中有 null 检查，安全降级 |
| 变异概率曲线值超范围 | 设计师配置错误 | OnValidate 中 Clamp 到 0~1 |
| 多个阈值同帧触发 | 大量SAN一次性消耗 | CheckMadnessThresholds 循环处理所有跨越的阈值 |
| NightSanityDrain 初始化顺序 | DayNightManager 可能尚未就绪 | 使用 DelayedInit 模式 + null 检查 |

### B. 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2026-03-07 | 初始策划文档完成 |

---

**文档结束**
