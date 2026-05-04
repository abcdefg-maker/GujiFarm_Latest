# 饥荒风格种地系统 - 完整策划框架

> **项目**: GuJi_Farm_Project
> **Unity 版本**: 2022.3.62f3c1 (LTS)
> **设计师**: Claude AI
> **日期**: 2026-01-30
> **版本**: v1.0
> **参考**: 《饥荒》(Don't Starve) 农业系统

---

## 目录

1. [系统概述](#1-系统概述)
2. [核心玩法循环](#2-核心玩法循环)
3. [系统架构设计](#3-系统架构设计)
4. [详细功能设计](#4-详细功能设计)
5. [数据结构设计](#5-数据结构设计)
6. [交互流程设计](#6-交互流程设计)
7. [数值平衡设计](#7-数值平衡设计)
8. [UI/UX设计](#8-uiux设计)
9. [技术实现方案](#9-技术实现方案)
10. [开发路线图](#10-开发路线图)

---

## 1. 系统概述

### 1.1 设计目标

创建一个**轻量级但深度足够**的农业系统，让玩家能够：
- 种植和收获各种作物
- 管理农田的状态（翻地、浇水、施肥）
- 体验作物生长的周期性和满足感
- 与游戏内时间系统产生关联

### 1.2 核心特色

- ✅ **简洁的交互**：继承项目已有的拾取系统，统一操作体验
- ✅ **可视化反馈**：作物生长阶段清晰可见
- ✅ **策略性**：需要规划种植时间、作物选择、资源分配
- ✅ **可扩展性**：架构支持后续添加高级功能（温室、自动化等）

### 1.3 参考《饥荒》的设计要素

| 饥荒特性 | 本项目实现方式 |
|---------|---------------|
| 农田地块 | FarmPlot系统，支持多种状态 |
| 作物生长阶段 | 4阶段可视化（种子→幼苗→成长→成熟） |
| 季节影响 | 简化为"适宜/不适宜"两档 |（wait）
| 需要浇水施肥 | 通过水分和肥力(wait)数值管理 |
| 腐烂机制 | 成熟后X天未收获自动枯萎 |（wait）
| 不同作物特性 | 通过ScriptableObject配置差异化属性 |

---

## 2. 核心玩法循环

### 2.1 基础循环（15分钟）

```
[获得种子] → [翻地准备] → [种植] → [浇水/施肥维护] → [等待生长] → [收获作物]
     ↑                                                              ↓
     └──────────────────────[出售/食用/再次种植]←─────────────────────┘
```

### 2.2 进阶玩法（可选扩展）（wait）

- **作物搭配**：不同作物搭配种植获得加成（wait）
- **稀有品种**：特殊作物需要特定条件（wait）
- **连作障碍**：同一地块连续种植同类作物降低产量（wait）
- **虫害系统**：需要保护措施（wait）
- **多季种植**：规划全年作物安排（wait）

---

## 3. 系统架构设计

### 3.1 系统分层架构

```
┌──────────────────────────────────────────────────────────────┐
│                      表现层 (Presentation)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │  FarmPlot   │  │  Crop       │  │  UI Panel   │          │
│  │  Visual     │  │  Sprite     │  │  Overlay    │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└────────────────────────┬─────────────────────────────────────┘
                         │ 渲染 & 事件
┌────────────────────────▼─────────────────────────────────────┐
│                      逻辑层 (Logic)                           │
│  ┌───────────────────────────────────────────────────────┐   │
│  │              FarmingManager (核心管理器)               │   │
│  │  - 管理所有农田状态                                    │   │
│  │  - 处理时间流逝更新                                    │   │
│  │  - 协调作物生长                                        │   │
│  └────────┬──────────────────────────────┬────────────────┘   │
│           │                              │                    │
│  ┌────────▼──────────┐        ┌─────────▼──────────┐        │
│  │  GrowthSystem     │        │  InteractionSystem │        │
│  │  - 生长计算       │        │  - 种植/收获逻辑   │        │
│  │  - 条件检查       │        │  - 维护操作        │        │
│  └───────────────────┘        └────────────────────┘        │
└────────────────────────┬─────────────────────────────────────┘
                         │ 使用
┌────────────────────────▼─────────────────────────────────────┐
│                      数据层 (Data)                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  CropDataSO  │  │  PlotState   │  │  TimeSystem  │       │
│  │  (配置数据)  │  │  (运行时)    │  │  (时间管理)  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└──────────────────────────────────────────────────────────────┘
```

### 3.2 核心类关系图

```
                    ┌─────────────┐
                    │   Player    │
                    └──────┬──────┘
                           │ 控制
              ┌────────────┼────────────┐
              │                         │
     ┌────────▼─────────┐      ┌───────▼────────┐
     │ ItemInteractor   │      │  FarmingTool   │
     │ (检测地块/作物)  │      │  (工具系统)    │
     └────────┬─────────┘      └───────┬────────┘
              │                         │
              │ 交互                交互 │
              │                         │
     ┌────────▼──────────────────────────▼─────────┐
     │         FarmPlot (地块对象)                 │
     │  ┌──────────────────────────────────────┐  │
     │  │  状态: Untilled/Ready/Planted/etc.   │  │
     │  │  持有: PlantedCrop (作物实例)        │  │
     │  │  属性: waterLevel, fertilityLevel    │  │
     │  └──────────────────────────────────────┘  │
     └──────────────────┬──────────────────────────┘
                        │ 拥有
              ┌─────────▼──────────┐
              │  PlantedCrop       │
              │  (运行时作物实例)  │
              │  - 生长进度        │
              │  - 健康状态        │
              │  - 引用CropDataSO  │
              └─────────┬──────────┘
                        │ 引用
              ┌─────────▼──────────┐
              │  CropDataSO        │
              │  (作物配置数据)    │
              │  - 生长时间        │
              │  - 产量范围        │
              │  - 季节要求        │
              │  - 视觉资源        │
              └────────────────────┘
```

### 3.3 与现有系统的集成

#### 集成点1: Item系统

```csharp
// 扩展已有的Item类
public class SeedItem : Item
{
    public CropDataSO cropData;  // 引用作物配置
    public override ItemType itemType => ItemType.Seed;
}

public class CropItem : Item
{
    public int harvestAmount;
    public override ItemType itemType => ItemType.Crop;
}
```

#### 集成点2: 拾取系统

```csharp
// 新增种植条件
[CreateAssetMenu(menuName = "Pickup/Conditions/Require Plot Ready")]
public class RequirePlotReadyCondition : ScriptablePickupCondition
{
    public override bool Check(Item item, GameObject picker, ItemPickupHandler handler)
    {
        // 检查地块是否已翻地
        var plot = item.GetComponent<FarmPlot>();
        return plot != null && plot.CurrentState == PlotState.Ready;
    }
}

// 新增种植效果
[CreateAssetMenu(menuName = "Pickup/Effects/Plant Seed")]
public class PlantSeedEffect : ScriptablePickupEffect
{
    public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
    {
        // 将种子种植到地块
        // ...
    }
}
```

#### 集成点3: 时间系统（新建）

```csharp
public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance;

    public int CurrentDay { get; private set; }
    public float CurrentHour { get; private set; }
    public Season CurrentSeason { get; private set; }

    // 订阅事件
    public event Action<int> OnDayPassed;
    public event Action<Season> OnSeasonChanged;
}
```

---

## 4. 详细功能设计

### 4.1 农田地块系统

#### 4.1.1 地块状态机

```
┌──────────────┐
│   Untilled   │  未翻地（初始状态）
│  (需要锄头)  │
└──────┬───────┘
       │ 使用锄头
       ▼
┌──────────────┐
│    Ready     │  已翻地，可种植
│  (可以种植)  │
└──────┬───────┘
       │ 种植种子
       ▼
┌──────────────┐
│   Planted    │  已种植
│  (作物生长中)│
└──────┬───────┘
       │ 作物成熟
       ▼
┌──────────────┐
│   Mature     │  成熟可收获
│  (可以收获)  │
└──────┬───────┘
       │ 收获/枯萎
       ▼
┌──────────────┐
│  Harvested   │  已收获
│  (回到Ready) │
└──────────────┘
       │
       ▼ 可再次种植或荒废回到Untilled
```

#### 4.1.2 地块属性

| 属性名称 | 类型 | 范围 | 说明 |
|---------|------|------|------|
| waterLevel | float | 0-100 | 水分值，影响生长速度 |
| fertilityLevel | float | 0-100 | 肥力值，影响产量 |
| growthSpeed | float | 0.5-2.0 | 生长速度倍率 |
| daysUnattended | int | 0+ | 无人维护天数 |
| plantedCrop | PlantedCrop | null或实例 | 当前种植的作物 |

#### 4.1.3 维护操作

**浇水 (Watering)**
- 消耗：无（或消耗水桶耐久）
- 效果：waterLevel +30，上限100
- 频率：每天1-2次
- 视觉反馈：地块颜色变深，水珠特效

**施肥 (Fertilizing)**
- 消耗：肥料物品×1
- 效果：fertilityLevel +50，上限100
- 频率：每个生长周期1-2次
- 视觉反馈：土壤泛光效果

**除草 (Weeding)**
- 消耗：无
- 效果：移除杂草，恢复growthSpeed到1.0
- 频率：杂草随机生成
- 视觉反馈：杂草消失动画

### 4.2 作物生长系统

#### 4.2.1 生长阶段

| 阶段 | 名称 | 进度范围 | 可交互操作      | 视觉表现 |
|-----|------|--------- |-----------     |---------|
| 0 | 种子   | 0%       | 浇水、施肥      | 土堆     |
| 1 | 发芽   | 0-25%    | 浇水、施肥、除草 | 小幼苗   |
| 2 | 生长   | 25-75%   | 浇水、施肥、除草 | 中等植株 |
| 3 | 成熟   | 75-100%  | 收获            | 果实明显 |
| 4 | 枯萎   | 超时      | 清除            | 删除贴图 |

#### 4.2.2 生长计算公式

```csharp
// 每天增长的进度百分比
float dailyGrowth = BaseGrowthRate
                  × (waterLevel / 100f)      // 水分影响 (0.0-1.0)
                  × (1 + fertilityLevel / 200f)  // 肥力加成 (1.0-1.5)
                  //× seasonMultiplier         // 季节倍率 (0.5-1.5)
                  //× growthSpeed;             // 地块状态倍率

// 防止生长过快或过慢
dailyGrowth = Mathf.Clamp(dailyGrowth, 0.05f, 0.5f);

// 更新进度
growthProgress += dailyGrowth;
growthProgress = Mathf.Clamp01(growthProgress);
```

#### 4.2.3 作物配置数据

```csharp
[CreateAssetMenu(menuName = "Farming/Crop Data")]
public class CropDataSO : ScriptableObject
{
    [Header("基础信息")]
    public string cropID;
    public string cropName;
    public string description;
    public Sprite icon;

    [Header("生长属性")]
    public int growthDays = 5;            // 基础生长天数
    public float baseGrowthRate = 0.2f;   // 每天生长20%

    [Header("收获属性")]
    public CropItem harvestPrefab;        // 收获的物品预制体
    public int minYield = 1;              // 最小产量
    public int maxYield = 3;              // 最大产量

    [Header("种植要求")]
    public Season[] preferredSeasons;     // 适宜季节
    public float minWaterLevel = 20f;     // 最低水分需求
    public float optimalWaterLevel = 60f; // 最佳水分

    [Header("视觉资源")]
    public GameObject[] growthStagePrefabs; // 4个生长阶段的模型

    [Header("特殊属性")]
    public bool isPerennial = false;      // 是否多季作物
    public int daysToWither = 3;          // 成熟后多少天枯萎
}
```

### 4.3 工具系统

#### 4.3.1 农具类型

| 工具 | 功能 | 耐久度 | 制作材料 | 特殊效果 |
|-----|------|--------|---------|---------|
| 木锄 | 翻地 | 50 | 木材×2 | 基础 |
| 铁锄 | 翻地 | 150 | 铁×2+木材×1 | 速度+20% |
| 水桶 | 浇水 | 100 | 木材×3 | - |


//| 洒水器 | 自动浇水 | ∞ | 铁×5+齿轮×2 | 范围3×3 |
//| 化肥 | 提升肥力 | - | 粪便×1+草×2 | 一次性消耗 |
//| 镰刀 | 快速收获 | 100 | 铁×2+木材×1 | 收获+1产量 |

#### 4.3.2 工具使用流程

```
[玩家手持工具] → [靠近目标地块] → [按F键交互]
    → [检查工具类型] → [执行对应操作] → [消耗耐久度]
        → [更新地块状态] → [播放反馈效果]
```

### 4.4 季节与天气系统

#### 4.4.1 季节设计

| 季节 | 持续天数 | 作物生长倍率 | 适宜作物 | 特殊事件 |
|-----|---------|-------------|---------|---------|
| 春季 | 16天 | 1.2× | 胡萝卜、卷心菜 | 多雨 |
| 夏季 | 16天 | 1.0× | 玉米、西瓜 | 干旱 |
| 秋季 | 16天 | 1.0× | 南瓜、土豆 | 多雨 |
| 冬季 | 16天 | 0.5× | 无（温室除外） | 霜冻 |

#### 4.4.2 天气影响

| 天气 | 概率 | 对地块的影响 | 对作物的影响 |
|-----|------|------------|-------------|
| 晴天 | 60% | waterLevel -10/天 | 正常生长 |
| 雨天 | 30% | waterLevel +30/天 | 生长+10% |
| 大雨 | 5% | waterLevel +50/天 | 可能倒伏（-20%生长） |
| 干旱 | 5% | waterLevel -30/天 | 生长-30% |

---

## 5. 数据结构设计

### 5.1 核心数据类

#### PlotState.cs

```csharp
[System.Serializable]
public class PlotState
{
    public string plotID;
    public PlotStatus status;

    // 状态属性
    public float waterLevel;
    public float fertilityLevel;
    public float growthSpeedModifier;

    // 作物信息
    public PlantedCrop currentCrop;

    // 维护信息
    public int daysUnwatered;
    public int daysUnfertilized;
    public bool hasWeeds;

    // 时间戳
    public int plantedDay;
    public int lastWateredDay;
}

public enum PlotStatus
{
    Untilled,    // 未翻地
    Ready,       // 已翻地
    Planted,     // 已种植
    Mature,      // 作物成熟
    Withered,    // 枯萎
    Depleted     // 地力耗尽
}
```

#### PlantedCrop.cs

```csharp
[System.Serializable]
public class PlantedCrop
{
    public CropDataSO cropData;        // 作物配置
    public float growthProgress;       // 0.0 - 1.0
    public int currentStage;           // 0-3
    public int daysGrowing;            // 已生长天数
    public float healthPoints;         // 健康值

    // 计算当前阶段
    public int GetCurrentStage()
    {
        if (growthProgress < 0.25f) return 0;
        if (growthProgress < 0.50f) return 1;
        if (growthProgress < 0.75f) return 2;
        return 3;
    }

    // 是否成熟
    public bool IsMature() => growthProgress >= 1.0f;
}
```

### 5.2 存档数据结构

```csharp
[System.Serializable]
public class FarmingSaveData
{
    public int version = 1;
    public int currentDay;
    public Season currentSeason;

    public List<PlotState> allPlots;
    public Dictionary<string, int> seedInventory;
    public Dictionary<string, int> cropInventory;

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static FarmingSaveData FromJson(string json)
    {
        return JsonUtility.FromJson<FarmingSaveData>(json);
    }
}
```

---

## 6. 交互流程设计

### 6.1 种植流程

```
Step 1: 翻地
  玩家手持【锄头】 → 靠近【未翻地地块】 → 按F键
    → 检查：地块状态==Untilled？
       → 是：播放翻地动画 → 消耗工具耐久1 → 地块状态→Ready
       → 否：提示"地块已翻地"

Step 2: 种植
  玩家手持【种子】 → 靠近【已翻地地块】 → 按F键
    → 检查：地块状态==Ready？手持物品是种子？
       → 是：播放种植动画 → 消耗种子×1 → 创建作物实例 → 地块状态→Planted
       → 否：提示相应错误

Step 3: 维护（可选，多次）
  玩家手持【水桶】 → 靠近【已种植地块】 → 按F键
    → 浇水：waterLevel +30
  玩家手持【肥料】 → 靠近【已种植地块】 → 按F键
    → 施肥：fertilityLevel +50 → 消耗肥料×1

Step 4: 等待生长
  TimeSystem每天触发OnDayPassed事件
    → FarmingManager遍历所有Planted地块
      → 计算生长进度 += dailyGrowth
        → 更新作物视觉阶段
          → 如果growthProgress >= 1.0 → 地块状态→Mature

Step 5: 收获
  玩家【空手或持镰刀】 → 靠近【成熟地块】 → 按F键
    → 检查：地块状态==Mature？
       → 是：播放收获动画 → 生成作物物品×(1-3个) → 地块状态→Ready
       → 否：提示"作物未成熟"
```

### 6.2 交互优先级设计

**按F键时的检测优先级**（从高到低）：

1. 成熟作物（可收获）→ 执行收获
2. 地块需要维护（浇水/施肥）→ 执行维护
3. 已翻地地块 + 手持种子 → 执行种植
4. 未翻地地块 + 手持锄头 → 执行翻地
5. 普通物品 → 执行原有的拾取逻辑

### 6.3 状态反馈设计

| 交互类型 | 音效 | 粒子特效 | UI提示 | 动画 |
|---------|------|---------|--------|------|
| 翻地 | 挖土声 | 土块飞溅 | "地块已翻地" | 玩家挥动锄头 |
| 种植 | 种植声 | 种子落地 | "已种植[作物名]" | 玩家下蹲动作 |
| 浇水 | 水声 | 水珠特效 | "水分+30" | 水流从桶中倒出 |
| 施肥 | 撒布声 | 肥料颗粒 | "肥力+50" | 玩家挥手动作 |
| 收获 | 收获声 | 光芒特效 | "+[数量]×[作物名]" | 作物消失并飞向玩家 |

---

## 7. 数值平衡设计

### 7.1 作物配置表

| 作物名称 | 生长天数 | 季节 | 种子价格 | 售价 | 产量 | 利润率 |
|---------|---------|------|---------|------|------|--------|
| 胡萝卜 | 3天 | 春/秋 | 10金 | 15金 | 1-2 | 50% |
| 卷心菜 | 5天 | 春 | 20金 | 40金 | 1 | 100% |
| 玉米 | 7天 | 夏 | 30金 | 60金 | 2-3 | 100-200% |
| 西瓜 | 10天 | 夏 | 50金 | 120金 | 1 | 140% |
| 南瓜 | 8天 | 秋 | 40金 | 100金 | 1-2 | 125-250% |
| 土豆 | 4天 | 春/秋 | 15金 | 30金 | 2-4 | 200-533% |

**设计原则**：
- 生长时间越长，单位时间收益越高
- 季节限定作物有更高利润率
- 多产作物价格相对较低

### 7.2 地块衰减曲线

```
100% |        ╱────────╲
     |      ╱            ╲
肥力  |    ╱                ╲
     |  ╱                    ╲
  0% |╱________________________╲_
     0   3   6   9   12  15  18  天

- 0-3天：肥力保持100%
- 3-6天：每天衰减5%
- 6-12天：每天衰减10%
- 12天后：每天衰减15%
```

### 7.3 收益计算示例

**场景**：玩家拥有9块地（3×3）

**种植方案1：快速周转**
- 全部种植胡萝卜（3天）
- 投入：10×9 = 90金
- 产出：15×13.5（平均1.5个） = 202.5金
- 净利：112.5金/3天 = 37.5金/天

**种植方案2：均衡发展**
- 3块胡萝卜 + 3块玉米 + 3块卷心菜
- 投入：30+90+60 = 180金
- 产出：45+150+120 = 315金
- 净利：135金/7天 = 19.3金/天（但分散风险）

---

## 8. UI/UX设计

### 8.1 HUD界面元素

```
┌─────────────────────────────────────────────────┐
│  [日期: 春季 第3天]  [时间: 12:00]  [天气: 晴] │
└─────────────────────────────────────────────────┘

                  [玩家视角]
                       ↓
    ┌─────────────────────────────┐
    │     [FarmPlot 悬浮UI]        │
    │  ┌───────────────────────┐  │
    │  │ 🌱 胡萝卜 - 生长中     │  │
    │  │ 进度: ▓▓▓▓░░░░ 60%    │  │
    │  │ 💧 水分: 45%          │  │
    │  │ 🌿 肥力: 80%          │  │
    │  │ ⏱ 预计成熟: 1天后     │  │
    │  └───────────────────────┘  │
    └─────────────────────────────┘

┌────────────────────────────────────────┐
│ [手持物品]                     [背包] │
│  🌾 胡萝卜种子 x5                 📦  │
└────────────────────────────────────────┘
```

### 8.2 地块状态指示器

**方案1：颜色编码**
- 未翻地：棕色暗淡
- 已翻地：棕色明亮
- 已种植：根据水分显示深浅
  - 高水分（>60%）：深棕色
  - 中水分（30-60%）：中棕色
  - 低水分（<30%）：浅棕色，边缘干裂
- 成熟：作物发光高亮

**方案2：图标悬浮**
```
地块上方悬浮小图标：
💧 = 需要浇水
🌿 = 需要施肥
🐛 = 有虫害
⚠️ = 即将枯萎
✅ = 可收获
```

### 8.3 教学引导

**第一次种植教程**：
```
Step 1: 提示框 "按住Tab查看农田信息"
  → 显示所有地块的详细状态

Step 2: 箭头指向地块 "使用锄头翻地"
  → 玩家完成后 → 提示消失

Step 3: 箭头指向翻好的地 "种植种子"
  → 玩家完成后 → 显示作物信息面板

Step 4: 时间快进到下一天
  → 提示 "作物需要水分！使用水桶浇水"

Step 5: 时间快进到成熟
  → 提示 "作物成熟了！收获它吧"
```

---

## 9. 技术实现方案

### 9.1 核心类实现

#### FarmingManager.cs

```csharp
public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance;

    [Header("配置")]
    public Transform farmPlotsParent;
    public List<FarmPlot> allPlots = new List<FarmPlot>();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 订阅时间系统事件
        TimeSystem.Instance.OnDayPassed += HandleDayPassed;

        // 初始化所有地块
        InitializePlots();
    }

    private void HandleDayPassed(int day)
    {
        foreach (var plot in allPlots)
        {
            if (plot.CurrentState == PlotStatus.Planted)
            {
                plot.UpdateGrowth();
            }

            // 水分自然衰减
            plot.DecreaseWater(10f);
        }
    }

    public void RegisterPlot(FarmPlot plot)
    {
        if (!allPlots.Contains(plot))
        {
            allPlots.Add(plot);
        }
    }
}
```

#### FarmPlot.cs

```csharp
public class FarmPlot : MonoBehaviour
{
    [Header("状态")]
    public PlotStatus CurrentState = PlotStatus.Untilled;
    public PlotState plotState = new PlotState();

    [Header("视觉")]
    public GameObject cropVisualParent;
    public Material untilledMaterial;
    public Material readyMaterial;

    private void Start()
    {
        FarmingManager.Instance.RegisterPlot(this);
        UpdateVisual();
    }

    // 翻地
    public bool Till()
    {
        if (CurrentState != PlotStatus.Untilled) return false;

        CurrentState = PlotStatus.Ready;
        plotState.status = PlotStatus.Ready;
        plotState.fertilityLevel = 100f;
        plotState.waterLevel = 50f;

        UpdateVisual();
        return true;
    }

    // 种植
    public bool Plant(CropDataSO cropData)
    {
        if (CurrentState != PlotStatus.Ready) return false;
        if (cropData == null) return false;

        plotState.currentCrop = new PlantedCrop
        {
            cropData = cropData,
            growthProgress = 0f,
            daysGrowing = 0,
            healthPoints = 100f
        };

        CurrentState = PlotStatus.Planted;
        plotState.plantedDay = TimeSystem.Instance.CurrentDay;

        UpdateVisual();
        return true;
    }

    // 浇水
    public void Water(float amount = 30f)
    {
        plotState.waterLevel = Mathf.Min(100f, plotState.waterLevel + amount);
        plotState.lastWateredDay = TimeSystem.Instance.CurrentDay;
        UpdateVisual();
    }

    // 施肥
    public void Fertilize(float amount = 50f)
    {
        plotState.fertilityLevel = Mathf.Min(100f, plotState.fertilityLevel + amount);
        UpdateVisual();
    }

    // 更新生长
    public void UpdateGrowth()
    {
        if (plotState.currentCrop == null) return;

        // 计算生长速度
        float waterFactor = plotState.waterLevel / 100f;
        float fertFactor = 1f + (plotState.fertilityLevel / 200f);
        float seasonFactor = GetSeasonMultiplier();

        float dailyGrowth = plotState.currentCrop.cropData.baseGrowthRate
                          * waterFactor
                          * fertFactor
                          * seasonFactor;

        plotState.currentCrop.growthProgress += dailyGrowth;
        plotState.currentCrop.daysGrowing++;

        // 检查是否成熟
        if (plotState.currentCrop.IsMature())
        {
            CurrentState = PlotStatus.Mature;
        }

        UpdateVisual();
    }

    // 收获
    public List<CropItem> Harvest()
    {
        if (CurrentState != PlotStatus.Mature) return null;

        var crop = plotState.currentCrop;
        int yield = Random.Range(crop.cropData.minYield, crop.cropData.maxYield + 1);

        // 肥力影响产量
        if (plotState.fertilityLevel > 80f)
        {
            yield += 1;
        }

        List<CropItem> harvested = new List<CropItem>();
        for (int i = 0; i < yield; i++)
        {
            var item = Instantiate(crop.cropData.harvestPrefab);
            harvested.Add(item);
        }

        // 重置地块
        plotState.currentCrop = null;
        CurrentState = PlotStatus.Ready;
        plotState.fertilityLevel -= 20f; // 收获后肥力下降

        UpdateVisual();
        return harvested;
    }

    private void UpdateVisual()
    {
        // 更新材质
        var renderer = GetComponent<Renderer>();
        if (CurrentState == PlotStatus.Untilled)
            renderer.material = untilledMaterial;
        else
            renderer.material = readyMaterial;

        // 更新作物视觉
        if (plotState.currentCrop != null)
        {
            int stage = plotState.currentCrop.GetCurrentStage();
            ShowCropStage(stage);
        }
        else
        {
            HideAllCropStages();
        }
    }

    private float GetSeasonMultiplier()
    {
        var season = TimeSystem.Instance.CurrentSeason;
        var crop = plotState.currentCrop.cropData;

        if (System.Array.Exists(crop.preferredSeasons, s => s == season))
            return 1.2f;
        else if (season == Season.Winter)
            return 0.5f;
        else
            return 1.0f;
    }
}
```

#### TimeSystem.cs

```csharp
public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance;

    [Header("时间设置")]
    public float dayLengthInSeconds = 300f; // 5分钟真实时间 = 1游戏天
    public bool isPaused = false;

    [Header("当前状态")]
    public int CurrentDay { get; private set; } = 1;
    public Season CurrentSeason { get; private set; } = Season.Spring;
    public float CurrentHour { get; private set; } = 6f;

    // 事件
    public event Action<int> OnDayPassed;
    public event Action<Season> OnSeasonChanged;
    public event Action<float> OnHourPassed;

    private float hourTimer = 0f;
    private float hourLength;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        hourLength = dayLengthInSeconds / 24f;
    }

    private void Update()
    {
        if (isPaused) return;

        hourTimer += Time.deltaTime;

        if (hourTimer >= hourLength)
        {
            hourTimer -= hourLength;
            AdvanceHour();
        }
    }

    private void AdvanceHour()
    {
        CurrentHour += 1f;
        OnHourPassed?.Invoke(CurrentHour);

        if (CurrentHour >= 24f)
        {
            CurrentHour = 0f;
            AdvanceDay();
        }
    }

    private void AdvanceDay()
    {
        CurrentDay++;
        OnDayPassed?.Invoke(CurrentDay);

        // 检查季节变化（每16天换季）
        if (CurrentDay % 16 == 0)
        {
            AdvanceSeason();
        }
    }

    private void AdvanceSeason()
    {
        CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
        OnSeasonChanged?.Invoke(CurrentSeason);
    }

    public void SkipToNextDay()
    {
        CurrentHour = 0f;
        AdvanceDay();
    }
}

public enum Season
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}
```

### 9.2 与拾取系统的集成

#### PlantSeedEffect.cs

```csharp
[CreateAssetMenu(fileName = "PlantSeed", menuName = "Pickup/Effects/Plant Seed")]
public class PlantSeedEffect : ScriptablePickupEffect
{
    public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
    {
        // 获取手持的种子
        var seed = handler.HeldItem as SeedItem;
        if (seed == null) return;

        // 获取目标地块
        var plot = item.GetComponent<FarmPlot>();
        if (plot == null || plot.CurrentState != PlotStatus.Ready) return;

        // 种植
        bool success = plot.Plant(seed.cropData);

        if (success)
        {
            // 消耗种子
            Destroy(seed.gameObject);
            handler.heldItem = null;

            Debug.Log($"Planted {seed.cropData.cropName}");
        }
    }
}
```

#### HarvestCropEffect.cs

```csharp
[CreateAssetMenu(fileName = "HarvestCrop", menuName = "Pickup/Effects/Harvest Crop")]
public class HarvestCropEffect : ScriptablePickupEffect
{
    public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
    {
        var plot = item.GetComponent<FarmPlot>();
        if (plot == null || plot.CurrentState != PlotStatus.Mature) return;

        // 收获作物
        List<CropItem> harvested = plot.Harvest();

        if (harvested != null)
        {
            foreach (var crop in harvested)
            {
                // 将作物添加到背包（后续实现）
                // 或直接掉落到地上
                crop.transform.position = plot.transform.position + Vector3.up * 0.5f;
            }

            Debug.Log($"Harvested {harvested.Count} crops");
        }
    }
}
```

### 9.3 性能优化建议

1. **对象池**
   - 作物视觉模型使用对象池
   - 粒子特效使用对象池

2. **批处理更新**
   - 不要每帧更新所有地块
   - 使用分帧更新（每帧更新N个地块）

3. **LOD系统**
   - 远离玩家的地块使用简化模型
   - 仅更新视野内的地块视觉

4. **数据分离**
   - 运行时数据和配置数据分离
   - 使用ScriptableObject存储静态数据

---

## 10. 开发路线图

### 阶段1：核心系统搭建

**Week 1: 基础框架**
- [ ] 创建TimeSystem脚本和测试
- [ ] 创建FarmingManager单例
- [ ] 创建FarmPlot基类和状态机
- [ ] 实现地块的状态切换（Untilled→Ready→Planted）

**Week 2: 作物生长**
- [ ] 创建CropDataSO配置系统
- [ ] 实现PlantedCrop生长计算逻辑
- [ ] 创建至少3种作物配置（胡萝卜、玉米、卷心菜）
- [ ] 实现作物视觉阶段切换

### 阶段2：交互系统

**Week 3: 工具集成**
- [ ] 创建HoeToolItem（锄头）
- [ ] 创建WaterBucketItem（水桶）
- [ ] 集成TillPlotEffect到拾取系统
- [ ] 集成WaterPlotEffect到拾取系统

**Week 4: 种植与收获**
- [ ] 创建SeedItem和CropItem类
- [ ] 集成PlantSeedEffect
- [ ] 集成HarvestCropEffect
- [ ] 测试完整的种植→收获循环

### 阶段3：深度内容

**Week 5: 作物多样性**
- [ ] 添加6-8种不同作物
- [ ] 实现季节系统影响
- [ ] 创建稀有作物和特殊要求

**Week 6: 维护机制**
- [ ] 实现施肥系统
- [ ] 实现杂草生成和清除
- [ ] 实现地力衰减机制
- [ ] 实现作物枯萎系统

**Week 7: 高级功能**
- [ ] 实现天气系统（基础版）
- [ ] 实现温室建筑（可选）
- [ ] 实现自动化工具（洒水器等）

### 阶段4：优化与完善（1周）

**Week 8: 打磨**
- [ ] UI美化和反馈优化
- [ ] 音效和粒子特效
- [ ] 性能优化（对象池、LOD）
- [ ] 存档系统集成
- [ ] Bug修复和平衡调整

### 里程碑检查点

| 里程碑 | 检查标准 | 预期时间 |
|-------|---------|---------|
| M1 | 可以翻地和看到时间流逝 | Week 1结束 |
| M2 | 可以种植种子并看到生长 | Week 2结束 |
| M3 | 可以使用工具进行维护 | Week 3结束 |
| M4 | 可以收获并获得产出 | Week 4结束 |
| M5 | 系统完整可玩 | Week 8结束 |

---

## 附录

### A. 术语表

| 术语 | 英文 | 定义 |
|-----|------|------|
| 地块 | Plot | 可以种植作物的土地单元 |
| 翻地 | Tilling | 将未耕作的土地准备成可种植状态 |
| 生长进度 | Growth Progress | 作物从0%到100%的成熟度 |
| 水分 | Water Level | 地块的含水量，影响生长速度 |
| 肥力 | Fertility Level | 地块的养分，影响产量 |
| 枯萎 | Wither | 作物成熟后未及时收获导致的死亡 |
| 地力耗尽 | Depleted | 地块因长期种植导致无法使用 |

### B. 参考资料

- 《饥荒》官方Wiki：https://dontstarve.fandom.com/wiki/Farming
- 《星露谷物语》农业系统分析
- Unity农业模拟教程

### C. 版本历史

| 版本 | 日期 | 变更内容 |
|-----|------|---------|
| v1.0 | 2026-01-30 | 初始策划文档完成 |

---

**文档结束**

这份策划框架提供了完整的设计蓝图，可以根据实际开发进度调整优先级和功能范围。建议从核心循环开始实现，逐步添加深度内容。
