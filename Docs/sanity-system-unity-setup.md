# SAN值系统 - Unity Editor 配置指南

> **Project**: GuJi_Farm_Project
> **Unity Version**: 2022.3.62f3c1 (LTS)
> **Date**: 2026-03-07
> **前置**: 代码已全部就位，本文档指导在 Unity Editor 中完成配置

---

## 目录

1. [配置前检查](#1-配置前检查)
2. [创建 SO 资产文件夹](#2-创建-so-资产文件夹)
3. [创建 SanityConfig SO](#3-创建-sanityconfig-so)
4. [创建 MutationTable SO](#4-创建-mutationtable-so)
5. [创建变异作物 CropData SO](#5-创建变异作物-cropdata-so)
6. [场景中创建 SanityManager](#6-场景中创建-sanitymanager)
7. [配置 NightSanityDrain](#7-配置-nightsanitydrain)
8. [将 NightSanityDrain 注册到 DayNightManager](#8-将-nightsanitydrain-注册到-daynightmanager)
9. [创建 SanityUI HUD](#9-创建-sanityui-hud)
10. [创建游戏结束面板](#10-创建游戏结束面板)
11. [验证测试](#11-验证测试)
12. [参数调优建议](#12-参数调优建议)

---

## 1. 配置前检查

确认以下脚本已存在且无编译错误：

```
Assets/Scripts/SanitySystem/
  ├── SanityConstants.cs
  ├── ISanityDrainSource.cs
  ├── IMadnessSymptom.cs
  ├── SanityConfigSO.cs
  ├── MutationTable.cs
  ├── SanityManager.cs
  ├── NightSanityDrain.cs
  └── SanityUI.cs
```

同时确认以下文件已修改：
- `Assets/Scripts/FarmingSystem/CropData.cs` — 底部应有 `isMutated` 和 `normalVariant` 字段
- `Assets/Scripts/FarmingSystem/SoilMound.cs` — `TryPlant` 方法中应有变异检查逻辑

**如果 Console 有编译错误，请先解决后再继续配置。**

---

## 2. 创建 SO 资产文件夹

1. 在 **Project** 窗口中，进入 `Assets/SO/`
2. 右键 → **Create → Folder**，命名为 `SanityData`

最终路径：`Assets/SO/SanityData/`

---

## 3. 创建 SanityConfig SO

这是 SAN 系统的核心配置文件，控制所有数值参数和曲线。

### 步骤

1. 在 `Assets/SO/SanityData/` 目录下
2. 右键 → **Create → Sanity → Sanity Config**
3. 命名为 `DefaultSanityConfig`
4. 选中该资产，在 Inspector 中配置参数

### Inspector 参数说明

#### SAN值

| 字段 | 默认值 | 说明 |
|------|--------|------|
| Max Sanity | 100 | SAN 最大值 |
| Starting Sanity | 100 | 游戏开始时的 SAN 值 |

#### 疯狂症状

| 字段 | 默认值 | 说明 |
|------|--------|------|
| Symptom Interval | 10 | 每降低多少点触发一次症状 |

#### 变异配置

| 字段 | 默认值 | 说明 |
|------|--------|------|
| Mutation Start Threshold | 80 | SAN 低于此值才可能出现变异 |
| Mutation Probability Curve | 见下 | X轴=归一化SAN, Y轴=变异概率 |

**变异概率曲线默认关键帧**（可在 Inspector 中可视化编辑）：

| X (归一化SAN) | Y (变异概率) | 含义 |
|--------------|-------------|------|
| 0.0 | 0.80 | SAN=0 时 80% 概率变异 |
| 0.5 | 0.20 | SAN=50 时 20% 概率 |
| 0.8 | 0.05 | SAN=80 时 5% 概率 |
| 1.0 | 0.00 | SAN=100 时不变异 |

> 提示：双击曲线字段可打开曲线编辑器，拖拽关键帧或添加新关键帧来调整曲线形状。

#### 夜间消耗

| 字段 | 默认值 | 说明 |
|------|--------|------|
| Night Drain Per Second | 0.5 | 每秒基础消耗量 |
| Night Drain Multiplier Curve | 线性 | X轴=nightLevel(0~1), Y轴=消耗倍率 |

**夜间消耗倍率曲线默认关键帧**：

| X (nightLevel) | Y (倍率) | 含义 |
|----------------|----------|------|
| 0.0 | 0.0 | 白天不消耗 |
| 0.5 | 0.5 | 半夜消耗 50% |
| 1.0 | 1.0 | 完全深夜消耗 100% |

#### 游戏结束

| 字段 | 默认值 | 说明 |
|------|--------|------|
| Game Over Delay | 2 | SAN 归零后延迟多少秒显示结束画面 |

---

## 4. 创建 MutationTable SO

变异映射表定义"哪些作物可以变异"以及"变异后是什么"。

### 步骤

1. 在 `Assets/SO/SanityData/` 目录下
2. 右键 → **Create → Sanity → Mutation Table**
3. 命名为 `MutationTable`
4. 在 Inspector 中配置 Entries 数组

### 配置 Entries

暂时可以留空（Entries 数组 Size = 0）。等变异作物 CropData 创建好后再回来配置。

每条 Entry 包含：

| 字段 | 说明 |
|------|------|
| Normal Crop | 拖入正常作物的 CropData SO（如 PotatoData） |
| Mutated Crop | 拖入变异版本的 CropData SO（如 MutatedPotatoData） |
| Probability Multiplier | 该品种的概率倍率（1.0=标准，>1更容易变异，<1更难） |

---

## 5. 创建变异作物 CropData SO

为每种需要变异的作物创建对应的变异版本 CropData。

### 示例：创建变异土豆

1. 在 `Assets/SO/CropsData/` 目录下
2. 右键 → **Create → Farming → Crop Data**
3. 命名为 `MutatedPotatoData`
4. 在 Inspector 中配置：

| 字段 | 值 | 说明 |
|------|-----|------|
| Crop ID | `crop_mutated_potato` | 唯一标识 |
| Crop Name | `变异土豆` | 显示名称 |
| Description | `被诡异力量侵蚀的土豆...` | 描述 |
| Days Per Stage | 1（同正常版本） | 生长速度 |
| Stage Sprites | **使用变异外观精灵** | 5张诡异/发光的生长图 |
| Harvest Item Prefab | 变异土豆的 Item Prefab | 收获物 |
| Is Mutated | **✅ 勾选** | 标记为变异作物 |
| Normal Variant | **拖入 PotatoData** | 引用其正常版本 |

> **重要**：`Is Mutated` 必须勾选，`Normal Variant` 必须指向正常版本的 CropData。

### 回填 MutationTable

变异 CropData 创建好后，回到 `MutationTable` 资产：

1. 选中 `Assets/SO/SanityData/MutationTable`
2. Entries → Size 设为 1（或更多）
3. 第一条 Entry：
   - Normal Crop → 拖入 `PotatoData`
   - Mutated Crop → 拖入 `MutatedPotatoData`
   - Probability Multiplier → `1.0`

> 提示：如果暂时没有变异作物的美术资源，可以先跳过此步。系统在没有变异映射时会安全降级（作物不会变异）。

---

## 6. 场景中创建 SanityManager

### 步骤

1. 打开 `Assets/Scenes/SampleScene.unity`
2. **Hierarchy** → 右键 → **Create Empty**
3. 命名为 `SanityManager`
4. 选中该 GameObject，在 Inspector 中：
   - 点击 **Add Component** → 搜索 `SanityManager` → 添加
5. 配置 Inspector 字段：

| 字段 | 操作 |
|------|------|
| Config | 拖入 `Assets/SO/SanityData/DefaultSanityConfig` |
| Mutation Table | 拖入 `Assets/SO/SanityData/MutationTable` |
| Symptom Behaviours | 暂时留空（Size = 0），后续添加症状效果时再配 |

---

## 7. 配置 NightSanityDrain

NightSanityDrain 可以挂在 SanityManager 同一 GameObject 上。

### 步骤

1. 选中 Hierarchy 中的 `SanityManager` GameObject
2. 点击 **Add Component** → 搜索 `NightSanityDrain` → 添加

无需额外配置，脚本会在运行时自动：
- 从 SanityManager 获取 SanityConfigSO 配置
- 注册到 SanityManager 作为消耗来源
- 注册到 DayNightManager 作为日夜效果插件

---

## 8. 将 NightSanityDrain 注册到 DayNightManager

NightSanityDrain 会在运行时通过代码自动注册到 DayNightManager，**无需手动拖入 effectBehaviours 列表**。

但如果你希望在 Inspector 中也可见（便于管理），也可以手动添加：

1. 找到场景中的 `DayNightManager` GameObject
2. 在 Inspector 中找到 **Effect Behaviours** 列表
3. 点击 `+` 添加一个槽位
4. 将 `SanityManager` GameObject（挂载了 NightSanityDrain 的那个）拖入该槽位

> 注意：即使不手动拖入，NightSanityDrain 的 `DelayedInit()` 也会调用 `DayNightManager.Instance.RegisterEffect(this)` 自动注册。两种方式选一种即可，不要重复配置（否则会注册两次，但代码有去重保护不会出错）。

---

## 9. 创建 SanityUI HUD

### 9.1 创建 UI 元素

1. 在 Hierarchy 中找到现有的 **HUD Canvas**（与 CurrencyUI 同一 Canvas）
2. 在 Canvas 下右键 → **UI → Panel**，命名为 `SanityPanel`
3. 调整位置（建议放在屏幕左上角或右上角，与金币显示区分）
4. 缩小 Panel 为合适的条状大小

### 9.2 添加数值文本

1. 在 `SanityPanel` 下右键 → **UI → Text - TextMeshPro**
2. 命名为 `SanityText`
3. 设置文本格式：
   - Font Size: 适当大小（如 24）
   - Alignment: Center
   - 默认文本: `100 / 100`

### 9.3 添加进度条（可选但推荐）

1. 在 `SanityPanel` 下右键 → **UI → Image**
2. 命名为 `SanityFillBar`
3. 设置 Image 属性：
   - **Image Type**: `Filled`
   - **Fill Method**: `Horizontal`
   - **Fill Origin**: `Left`
   - Fill Amount 会被脚本控制

### 9.4 挂载 SanityUI 脚本

1. 选中 `SanityPanel`
2. **Add Component** → 搜索 `SanityUI` → 添加
3. 配置 Inspector：

| 字段 | 操作 |
|------|------|
| Sanity Text | 拖入 `SanityText` TMP 组件 |
| Sanity Fill Bar | 拖入 `SanityFillBar` Image 组件（可选） |
| Bar Color Gradient | 编辑渐变色（默认已有红→黄→绿） |
| Game Over Panel | 先创建（见下一步），再拖入 |
| Game Over Text | 先创建（见下一步），再拖入（可选） |

---

## 10. 创建游戏结束面板

### 步骤

1. 在 HUD Canvas 下右键 → **UI → Panel**，命名为 `GameOverPanel`
2. 设置为**全屏覆盖**：
   - Anchors: Stretch-Stretch（四角拉满）
   - Left/Right/Top/Bottom 全部设为 0
3. 设置背景色为半透明黑色（如 RGBA: 0, 0, 0, 200）
4. **默认隐藏**：取消勾选 `GameOverPanel` 的 Active 复选框（脚本 Start 中也会自动隐藏）

### 添加结束文案

1. 在 `GameOverPanel` 下右键 → **UI → Text - TextMeshPro**
2. 命名为 `GameOverText`
3. 设置：
   - 文本内容（脚本会覆盖，可留空或写占位）
   - Font Size: 36~48
   - Alignment: Center
   - Color: 白色或红色
   - 居中于面板

### 关联到 SanityUI

1. 回到 `SanityPanel` 的 `SanityUI` 组件
2. 将 `GameOverPanel` 拖入 **Game Over Panel** 字段
3. 将 `GameOverText` 拖入 **Game Over Text** 字段（可选）

---

## 11. 验证测试

### 测试1：SAN 数值显示

1. 点击 **Play** 运行游戏
2. 确认 HUD 上显示 `100 / 100`（或配置的初始值）
3. Console 应输出：
   ```
   [SanityManager] 注册消耗来源: NightDrain
   [NightSanityDrain] 初始化完成
   [SanityUI] 初始化完成
   ```

### 测试2：夜晚 SAN 消耗

1. 运行游戏，等待进入夜晚（或在 TimeManager 中手动调快时间）
2. 进入夜晚后，观察 SAN 数值是否开始缓慢下降
3. Console 应输出阈值跨越信息：
   ```
   [SanityManager] 跨越疯狂阈值: 90 (SAN: 91 → 89)
   ```

### 测试3：作物变异

1. 确保 MutationTable 中配置了至少一条变异映射
2. 在 Console 中手动调低 SAN（临时在 SanityManager 中添加测试按键，或使用 Inspector Debug 模式修改 `currentSanity`）
3. 当 SAN < 80 时，种植对应作物
4. 观察是否有概率变异，Console 应输出：
   ```
   [SanityManager] 作物变异! 土豆 → 变异土豆 (概率: 20%, SAN: 50)
   ```

### 测试4：游戏结束

1. 将 SAN 降至 0（Debug 模式或快速消耗）
2. 确认：
   - Console 输出 `[SanityManager] 游戏结束 - 理智归零!`
   - GameOverPanel 显示
   - 游戏时间冻结（TimeManager 暂停）

### 快速调试技巧

在 Unity Editor 的 **Inspector → Debug** 模式下，可以直接修改 SanityManager 的 `currentSanity` 字段来测试不同 SAN 值的效果。步骤：

1. 运行游戏
2. 选中 `SanityManager` GameObject
3. Inspector 右上角菜单 → 切换到 **Debug** 模式
4. 直接修改 `currentSanity` 的值

> 注意：直接修改字段不会触发事件和阈值检测。如需触发，应调用 `DrainSanity()` 方法。可以临时添加一个测试脚本：

```csharp
// 临时测试脚本，测试完记得删除
using UnityEngine;
using SanitySystem;

public class SanityDebug : MonoBehaviour
{
    void Update()
    {
        // 按 F1 消耗 10 点 SAN
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SanityManager.Instance?.DrainSanity(10f);
            Debug.Log($"[Debug] SAN: {SanityManager.Instance?.CurrentSanity}");
        }

        // 按 F2 恢复 10 点 SAN
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SanityManager.Instance?.RestoreSanity(10f);
            Debug.Log($"[Debug] SAN: {SanityManager.Instance?.CurrentSanity}");
        }
    }
}
```

---

## 12. 参数调优建议

### 夜晚消耗节奏

以默认配置（nightDrainPerSecond = 0.5）为例：

| 场景 | nightLevel | 每秒消耗 | 每分钟消耗 |
|------|-----------|----------|-----------|
| 傍晚初期 | 0.3 | 0 | 0（低于0.5阈值） |
| 入夜 | 0.5 | 0.125 | 7.5 |
| 深夜 | 0.8 | 0.32 | 19.2 |
| 完全深夜 | 1.0 | 0.5 | 30 |

一个完整夜晚（约5分钟游戏内时间）大约消耗 **10~15 SAN**。

如果觉得太快/太慢，调整 `Night Drain Per Second` 即可。

### 变异概率手感

| SAN 值 | 默认概率 | 体验描述 |
|--------|---------|----------|
| 100~80 | 0% | 安全区，正常游玩 |
| 80~60 | 5%~10% | 偶尔变异，引起警觉 |
| 60~40 | 10%~30% | 频繁变异，压力明显 |
| 40~20 | 30%~60% | 大量变异，需要尽快恢复 |
| 20~0 | 60%~80% | 几乎必定变异，危险区 |

---

## 配置完成清单

- [ ] `Assets/SO/SanityData/DefaultSanityConfig.asset` 已创建并配置
- [ ] `Assets/SO/SanityData/MutationTable.asset` 已创建
- [ ] 场景中 `SanityManager` GameObject 已创建，挂载 SanityManager + NightSanityDrain
- [ ] SanityManager 的 Config 和 MutationTable 已拖入
- [ ] NightSanityDrain 已注册到 DayNightManager（自动或手动）
- [ ] HUD Canvas 中 SanityUI 已配置，文本和进度条已拖入
- [ ] GameOverPanel 已创建并默认隐藏，已拖入 SanityUI
- [ ] Play 测试通过，Console 无报错

---

**文档结束**
