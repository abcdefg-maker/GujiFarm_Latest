# 商店系统 & 背包扩展 实现文档

> **Project**: GuJi_Farm_Project
> **Unity Version**: 2022.3.62f3c1 (LTS)
> **Date**: 2026-02-25
> **Version**: v1.0

---

## 1. 概述

本次实现包含两部分：

1. **物品栏扩展**：将原有10个快捷栏槽位扩展为50个（10快捷栏 + 40背包），新增独立背包界面（B键开关）
2. **商店系统**：NPC交互触发的完整买卖系统，左右并排布局（左=商品购买，右=物品出售）

---

## 2. 修改的现有文件（3个）

### 2.1 GameConstants.cs

**路径**: `Assets/Scripts/Core/Constants/GameConstants.cs`

**改动内容**:
- `DefaultMaxSlots` 从 `10` 改为 `50`（总槽位数）
- 新增 `HotbarSlots = 10`（快捷栏槽位数）
- 新增 `BackpackSlots = 40`（背包槽位数）
- 新增 `#region Shop` 区域：
  - `DefaultStartingGold = 500` — 初始金币
  - `ShopDropScatterRadius = 1.5f` — 背包满时物品掉落散布半径
  - `TransactionMessageDuration = 2f` — 交易提示显示时长

### 2.2 Item.cs

**路径**: `Assets/Scripts/Item.cs`

**改动内容**:
- 在 `[Header("物品栏显示")]` 和 `[Header("拾取设置")]` 之间新增：
```csharp
[Header("经济属性")]
[Tooltip("出售价格，0表示不可出售")]
public int sellPrice = 0;
```
- 所有物品预制体都可以在Inspector中配置出售价格
- CropItem已有自己的sellPrice字段会覆盖基类

### 2.3 InventoryManager.cs

**路径**: `Assets/Scripts/InventorySystem/InventoryManager.cs`

**改动内容**:
- 新增 `using Core.Constants;`
- `maxSlots` 默认值从 `10` 改为 `GameConstants.DefaultMaxSlots`（50）
- 新增属性 `HotbarSize`（返回10）
- 新增属性 `BackpackStartIndex`（返回10，背包从第10个槽位开始）
- 现有InventoryUI无需改动（仍只显示10个快捷栏）

---

## 3. 新建文件清单（13个）

所有文件位于 `Assets/Scripts/ShopSystem/` 目录下：

| # | 文件 | 职责 |
|---|------|------|
| 1 | CurrencyManager.cs | 金币管理器单例 |
| 2 | ShopItemEntry.cs | 商品条目数据类 |
| 3 | ShopData.cs | 商店配置 ScriptableObject |
| 4 | ShopManager.cs | 核心交易逻辑单例 |
| 5 | ShopUI.cs | 商店根面板控制器 |
| 6 | ShopBuyPanelUI.cs | 左侧购买面板 |
| 7 | ShopItemSlotUI.cs | 单个商品卡片UI |
| 8 | ShopSellPanelUI.cs | 右侧出售面板 |
| 9 | ShopInventorySlotUI.cs | 物品栏槽位UI（可点击选中） |
| 10 | QuantityDialogUI.cs | 数量选择弹窗 |
| 11 | CurrencyUI.cs | HUD金币常驻显示 |
| 12 | BackpackUI.cs | 独立背包界面（B键开关） |
| 13 | ShopTrigger.cs | 商店NPC触发器 |

---

## 4. 各脚本详细说明

### 4.1 CurrencyManager.cs — 金币管理器

**类型**: 单例 MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| startingGold | int | 500 | 初始金币数 |

**公开属性**: `CurrentGold`（当前金币数）

**公开事件**: `OnGoldChanged(int)` — 金币变化时触发

**公开方法**:
- `AddGold(int amount)` — 增加金币
- `SpendGold(int amount) : bool` — 消费金币，不足返回false
- `HasEnoughGold(int amount) : bool` — 检查是否有足够金币

---

### 4.2 ShopItemEntry.cs — 商品条目数据

**类型**: `[System.Serializable]` 数据类（不是 MonoBehaviour）

**字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| itemPrefab | GameObject | 物品预制体 |
| displayName | string | 显示名称（空则从Prefab读取） |
| icon | Sprite | 商品图标（空则从Prefab读取） |
| buyPrice | int | 购买价格 |

**方法**: `GetDisplayName()`, `GetIcon()`, `GetItemID()` — 优先用自身字段，空则从Prefab的Item组件读取

---

### 4.3 ShopData.cs — 商店配置

**类型**: ScriptableObject（`[CreateAssetMenu(menuName = "Shop/Shop Data")]`）

**字段**:
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| shopName | string | "神秘商店" | 商店名称 |
| items | ShopItemEntry[] | — | 商品列表 |
| acceptedSellTypes | ItemType[] | Crop,Material,Seed | 接受出售的物品类型 |
| sellPriceMultiplier | float | 1.0 | 出售价格倍率（0.1~2.0） |

**方法**: `CanSellItemType(ItemType) : bool` — 检查是否接受该类型物品出售

---

### 4.4 ShopManager.cs — 核心交易逻辑

**类型**: 单例 MonoBehaviour

**公开属性**: `Catalog`（当前商店数据）, `IsOpen`（商店是否打开）

**公开事件**:
| 事件 | 参数 | 触发时机 |
|------|------|----------|
| OnShopOpened | — | 商店打开 |
| OnShopClosed | — | 商店关闭 |
| OnItemBought | ShopItemEntry, int | 成功购买 |
| OnItemSold | int, int | 成功出售(槽位, 数量) |
| OnTransactionFailed | string | 交易失败(原因) |

**公开方法**:
- `OpenShop(ShopData, Transform)` — 打开商店，参数为商店配置和NPC位置
- `CloseShop()` — 关闭商店
- `BuyItem(ShopItemEntry, int amount) : bool` — 购买物品
  - 扣金币 → 实例化Prefab → 加入背包 → 背包满则掉落在NPC附近
- `SellItem(int slotIndex, int amount) : bool` — 出售物品（**无限收购，物品直接Destroy**）
  - 检查类型 → 计算价格 → 移除物品 → 加金币 → 销毁物品GameObject
- `GetSellPrice(Item) : int` — 查询物品出售价格

---

### 4.5 ShopUI.cs — 商店根面板控制器

**类型**: MonoBehaviour

> **重要**: 此脚本必须挂在一个**始终激活的父物体**上（如ShopRoot），shopPanel引用指向子面板。否则面板隐藏后Update不运行，无法检测ESC按键。

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| shopPanel | GameObject | 商店面板（被显示/隐藏的子物体） |
| buyPanel | ShopBuyPanelUI | 左侧购买面板 |
| sellPanel | ShopSellPanelUI | 右侧出售面板 |
| closeButton | Button | 关闭按钮（左上角返回箭头） |
| shopTitleText | TextMeshProUGUI | 标题文本 |

**行为**:
- 订阅 `ShopManager.OnShopOpened` → 显示面板 + 刷新子面板
- 订阅 `ShopManager.OnShopClosed` → 隐藏面板
- `Update()` 中检测 ESC 键 → 关闭商店
- closeButton 点击 → 关闭商店

---

### 4.6 ShopBuyPanelUI.cs — 左侧购买面板

**类型**: MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| gridParent | Transform | 商品网格容器（ScrollView的Content） |
| slotPrefab | GameObject | 商品卡片Prefab（需挂ShopItemSlotUI） |

**行为**:
- `RefreshUI()` — 清空旧卡片，遍历ShopData.items动态生成商品卡片
- 点击商品卡片 → 计算最大可买数量
  - 只能买1个 → 直接购买
  - 能买多个 → 打开QuantityDialogUI选数量

---

### 4.7 ShopItemSlotUI.cs — 商品卡片UI

**类型**: MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| iconImage | Image | 物品图标 |
| nameText | TextMeshProUGUI | 物品名称 |
| coinIcon | Image | 金币图标 |
| priceText | TextMeshProUGUI | 价格文本 |
| buyButton | Button | 购买按钮（自动查找GetComponent） |

**自动查找子物体名称**: `Icon`, `Name`, `CoinIcon`, `Price`

**事件**: `OnBuyClicked(ShopItemEntry)` — 点击购买时触发

---

### 4.8 ShopSellPanelUI.cs — 右侧出售面板

**类型**: MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| backpackGridParent | Transform | 背包网格容器（上方，40格） |
| hotbarGridParent | Transform | 快捷栏网格容器（下方，10格） |
| slotPrefab | GameObject | 槽位Prefab（需挂ShopInventorySlotUI） |
| goldText | TextMeshProUGUI | 金币数显示 |
| sellButton | Button | 出售按钮 |

**行为**:
- 初始化时在两个容器中动态生成50个槽位UI（快捷栏0-9 + 背包10-49）
- 点击槽位 → 高亮选中（再次点击取消）
- 点击出售按钮 → 出售选中物品（数量>1时弹出数量选择）
- 订阅 `InventoryManager.OnSlotChanged` 实时更新
- 订阅 `CurrencyManager.OnGoldChanged` 更新金币显示

---

### 4.9 ShopInventorySlotUI.cs — 物品栏槽位UI

**类型**: MonoBehaviour（**背包和商店共用**的复用组件）

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| iconImage | Image | 物品图标 |
| countText | TextMeshProUGUI | 数量文本 |
| highlightImage | Image | 选中高亮 |
| backgroundImage | Image | 背景 |
| slotButton | Button | 点击按钮 |
| normalColor | Color | 正常颜色 |
| highlightColor | Color | 高亮颜色 |

**自动查找子物体名称**: `Icon`, `Highlight`（backgroundImage和Button通过GetComponent获取自身）

**公开方法**:
- `Initialize(int index)` — 初始化并自动查找组件
- `SetSlot(InventorySlot)` — 显示物品图标和数量
- `SetHighlight(bool)` — 设置选中高亮
- `Clear()` — 清空显示

**事件**: `OnSlotClicked(int slotIndex)` — 点击时触发

---

### 4.10 QuantityDialogUI.cs — 数量选择弹窗

**类型**: MonoBehaviour（静态单例访问）

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| dialogPanel | GameObject | 弹窗面板 |
| itemIcon | Image | 物品图标 |
| itemNameText | TextMeshProUGUI | 物品名称 |
| quantityText | TextMeshProUGUI | 数量显示 |
| decreaseButton | Button | 减少按钮 [-] |
| increaseButton | Button | 增加按钮 [+] |
| confirmButton | Button | 确认按钮 |
| cancelButton | Button | 取消按钮 |

**静态方法**: `QuantityDialogUI.Show(name, icon, min, max, callback)` — 显示弹窗

---

### 4.11 CurrencyUI.cs — HUD金币常驻显示

**类型**: MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| goldText | TextMeshProUGUI | 金币数文本 |

**行为**: 订阅 `CurrencyManager.OnGoldChanged` 实时更新显示

---

### 4.12 BackpackUI.cs — 独立背包界面

**类型**: MonoBehaviour

> **重要**: 此脚本必须挂在一个**始终激活的父物体**上（如BackpackRoot），backpackPanel引用指向子面板。否则面板隐藏后Update不运行，无法检测B按键。

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| backpackPanel | GameObject | 背包面板（被显示/隐藏的子物体） |
| gridParent | Transform | 网格容器 |
| slotPrefab | GameObject | 槽位Prefab（需挂ShopInventorySlotUI） |
| closeButton | Button | 关闭按钮 |

**行为**:
- `Update()` 检测B键 → 切换显示/隐藏
- 商店打开时（`ShopManager.IsOpen`）B键无效
- 首次打开时动态生成40个槽位（对应InventoryManager槽位10-49）
- 背包为**只读查看**，不提供出售功能
- 订阅 `InventoryManager.OnSlotChanged` 实时更新

---

### 4.13 ShopTrigger.cs — NPC触发器

**类型**: MonoBehaviour

**SerializeField字段**:
| 字段 | 类型 | 说明 |
|------|------|------|
| shopData | ShopData | 商店配置SO |
| interactHint | GameObject | 交互提示UI（可选） |

**行为**:
- NPC需要 Collider（勾选 isTrigger）
- 玩家进入范围 → 显示交互提示
- 按E键 → 打开/关闭商店
- 玩家离开范围 → 自动关闭商店

**前提**: 玩家物体的Tag必须设为 `Player`

---

## 5. Unity 配置步骤

### 5.1 前置准备

确保场景中已有以下物体：
- 挂载 `InventoryManager` 的物体（原有）
- 玩家物体的Tag为 `Player`（原有）

---

### 5.2 创建 ShopData 资产

1. Project窗口右键 → **Create → Shop → Shop Data**
2. 重命名为你想要的商店名（如 `MysteryShopData`）
3. 在Inspector中配置：
   - **Shop Name**: "神秘商店"
   - **Items**: 点击 + 添加商品条目
     - 每条填写：Item Prefab（拖入物品预制体）、Buy Price（购买价格）
     - Display Name 和 Icon 可不填（自动从Prefab读取）
   - **Accepted Sell Types**: 默认 Crop, Material, Seed（根据需要调整）
   - **Sell Price Multiplier**: 1.0（出售价格倍率）

---

### 5.3 制作槽位 Prefab（ShopInventorySlotUI）

> 此Prefab被**背包界面**和**商店出售面板**共用，只需做一次。

**Hierarchy 结构**:
```
BackpackSlot        ← Image(背景) + Button + ShopInventorySlotUI
├── Icon            ← Image（物品图标）
├── Highlight       ← Image（选中高亮）
└── Count           ← TextMeshPro（数量文字）
```

**详细步骤**:

1. Canvas下右键 → UI → **Image**，重命名为 `BackpackSlot`
   - Image组件：颜色设为半透明深色（如 RGBA 50,50,50,200）
   - RectTransform：大小 **60×60**
   - 添加组件 → **Button**
   - 添加组件 → **ShopInventorySlotUI**（无需手动绑定字段，代码会自动查找）

2. `BackpackSlot` 下右键 → UI → **Image**，重命名为 **Icon**（名称必须精确）
   - Anchor：Stretch All（四边各留4px padding）
   - 取消勾选 enabled（初始隐藏）

3. `BackpackSlot` 下右键 → UI → **Image**，重命名为 **Highlight**（名称必须精确）
   - Anchor：Stretch All（与父物体同大小）
   - 颜色：半透明白色（RGBA 255,255,255,80）
   - 取消勾选 enabled（初始隐藏）

4. `BackpackSlot` 下右键 → UI → Text - TextMeshPro，重命名为 `Count`
   - Anchor：右下角
   - 字号：12-14
   - 颜色：白色
   - 取消勾选 enabled（数量>1时代码才显示）

5. 将 `BackpackSlot` 拖入 Project 窗口制作成 **Prefab**，然后从场景删除

---

### 5.4 制作商品卡片 Prefab（ShopItemSlotUI）

> 此Prefab用于商店左侧购买面板的商品展示。

**Hierarchy 结构**:
```
ShopItemSlot        ← Image(卡片背景) + Button + ShopItemSlotUI
├── Icon            ← Image（商品图标）
├── Name            ← TextMeshPro（商品名称）
├── CoinIcon        ← Image（金币小图标）
└── Price           ← TextMeshPro（价格数字）
```

**详细步骤**:

1. Canvas下右键 → UI → **Image**，重命名为 `ShopItemSlot`
   - Image组件：卡片背景色
   - RectTransform：大小约 **200×80**（宽×高，适合2列布局）
   - 添加组件 → **Button**
   - 添加组件 → **ShopItemSlotUI**

2. 子物体 **Icon**（Image）：左侧，大小 60×60，显示商品图标

3. 子物体 **Name**（TextMeshPro）：Icon右侧上方，显示商品名称

4. 子物体 **CoinIcon**（Image）：Icon右侧下方左，小金币图标（拖入`玩家金币.png`）

5. 子物体 **Price**（TextMeshPro）：CoinIcon右侧，显示价格数字

6. 拖入 Project 制作成 **Prefab**，从场景删除

---

### 5.5 配置独立背包界面

**Hierarchy 结构**:
```
BackpackRoot        ← 空物体，挂 BackpackUI（始终激活！）
└── BackpackPanel   ← 实际面板（被代码控制显示/隐藏）
    ├── Background  ← Image（背景图）
    ├── TitleText   ← TextMeshPro（"背包"）
    ├── BackpackGrid ← 空物体 + Grid Layout Group
    └── CloseButton ← Button（关闭按钮）
```

**详细步骤**:

1. **Canvas下**右键 → Create Empty → 重命名为 `BackpackRoot`
   - 添加组件 → **BackpackUI**
   - **保持激活状态**（始终active）

2. `BackpackRoot` 下右键 → Create Empty → 重命名为 `BackpackPanel`
   - RectTransform：居中，大小约 **550×400**
   - **设为未激活**（取消Inspector左上角active勾选）

3. `BackpackPanel` 下创建子物体：
   - **Background**（Image）：Stretch All，设置背景色或图片
   - **TitleText**（TextMeshPro）：Anchor顶部居中，文本"背包"，字号24
   - **BackpackGrid**（Create Empty）：
     - 添加组件 → **Grid Layout Group**
       - Cell Size: 60×60
       - Spacing: 4×4
       - **Constraint: Fixed Column Count = 8**
       - Start Corner: Upper Left
     - RectTransform：在Background内留出标题空间
   - **CloseButton**（Button）：右上角，大小30×30，显示"X"

4. 选中 `BackpackRoot`，在 **BackpackUI** 组件中绑定字段：

| Inspector字段 | 拖入 |
|---------------|------|
| Backpack Panel | `BackpackPanel`（子物体） |
| Grid Parent | `BackpackGrid` |
| Slot Prefab | `BackpackSlot` Prefab（5.3中制作的） |
| Close Button | `CloseButton` |

> **运行效果**：按B键打开/关闭背包。代码自动在BackpackGrid下生成40个槽位（8列×5行）。商店打开时B键无效。

---

### 5.6 配置场景管理器物体

1. Hierarchy右键 → Create Empty → 重命名为 `CurrencyManager`
   - 添加组件 → **CurrencyManager**
   - Starting Gold：500（或自定义）

2. Hierarchy右键 → Create Empty → 重命名为 `ShopManager`
   - 添加组件 → **ShopManager**

---

### 5.7 配置商店UI面板

**Hierarchy 结构**（全部在 Canvas 下）:
```
ShopRoot                    ← 空物体，挂 ShopUI（始终激活！）
└── ShopPanel               ← 实际面板（初始未激活）
    ├── CloseButton         ← Button（左上角，返回.png）
    ├── TitleImage          ← Image（顶部居中，商店标题.png）
    │   └── TitleText       ← TextMeshPro（商店名称）
    ├── BuyPanel            ← 左侧购买面板
    │   └── ...
    └── SellPanel           ← 右侧出售面板
        └── ...
```

#### 5.7.1 根结构

1. **Canvas下**右键 → Create Empty → 重命名为 `ShopRoot`
   - 添加组件 → **ShopUI**
   - **保持激活状态**

2. `ShopRoot` 下 → Create Empty → 重命名为 `ShopPanel`
   - RectTransform：Stretch All（覆盖全屏）
   - 可添加Image作为半透明遮罩（RGBA 0,0,0,128）
   - **设为未激活**

3. `ShopPanel` 下创建：
   - **CloseButton**（Button）：左上角，拖入 `返回.png` 作为Image
   - **TitleImage**（Image）：顶部居中，拖入 `商店标题.png`
     - 子物体 **TitleText**（TextMeshPro）：显示商店名（代码自动设置）

#### 5.7.2 左侧购买面板（BuyPanel）

```
BuyPanel                ← Image(商店背景.png) + ShopBuyPanelUI
└── ScrollView          ← UI → Scroll View
    └── Viewport
        └── Content     ← Grid Layout Group（商品卡片容器）
```

1. `ShopPanel` 下右键 → UI → **Image** → 重命名为 `BuyPanel`
   - 添加组件 → **ShopBuyPanelUI**
   - Anchor：左半边（Left=0, Right=0.5）
   - Image：拖入 `商店背景.png`

2. `BuyPanel` 下右键 → UI → **Scroll View**
   - 删除 Horizontal Scrollbar（只保留垂直滚动）
   - Viewport → Content 上添加 **Grid Layout Group**:
     - Cell Size: 200×80（和商品卡片Prefab一致）
     - **Constraint: Fixed Column Count = 2**
     - Spacing: 8×8
   - Content 上添加 **Content Size Fitter**:
     - Vertical Fit: Preferred Size

3. 选中 `BuyPanel`，在 **ShopBuyPanelUI** 组件中绑定：

| Inspector字段 | 拖入 |
|---------------|------|
| Grid Parent | ScrollView → Viewport → `Content` |
| Slot Prefab | `ShopItemSlot` Prefab（5.4中制作的） |

#### 5.7.3 右侧出售面板（SellPanel）

```
SellPanel                   ← Image(青色背景) + ShopSellPanelUI
├── BackpackGrid            ← 空物体 + Grid Layout Group（上方，40格）
├── HotbarGrid              ← 空物体 + Grid Layout Group（下方，10格）
└── BottomBar               ← 底部栏
    ├── CoinIcon            ← Image（玩家金币.png）
    ├── GoldText            ← TextMeshPro（金币数）
    └── SellButton          ← Button（"出售"）
```

1. `ShopPanel` 下右键 → UI → **Image** → 重命名为 `SellPanel`
   - 添加组件 → **ShopSellPanelUI**
   - Anchor：右半边（Left=0.5, Right=1）
   - Image颜色：青色系

2. `SellPanel` 下创建 **BackpackGrid**（Create Empty）:
   - Grid Layout Group: Cell Size 60×60, Spacing 4×4, **Fixed Column Count = 8**
   - 位置：上方大区域（占面板约70%高度）

3. `SellPanel` 下创建 **HotbarGrid**（Create Empty）:
   - Grid Layout Group: Cell Size 60×60, Spacing 4×4, **Fixed Column Count = 10**（或5）
   - 位置：BackpackGrid下方

4. `SellPanel` 下创建 **BottomBar**（Create Empty）:
   - 底部横条布局
   - 子物体 **CoinIcon**（Image）：拖入 `玩家金币.png`
   - 子物体 **GoldText**（TextMeshPro）：显示金币数
   - 子物体 **SellButton**（Button）：文本"出售"

5. 选中 `SellPanel`，在 **ShopSellPanelUI** 组件中绑定：

| Inspector字段 | 拖入 |
|---------------|------|
| Backpack Grid Parent | `BackpackGrid` |
| Hotbar Grid Parent | `HotbarGrid` |
| Slot Prefab | `BackpackSlot` Prefab（5.3中制作的，同一个） |
| Gold Text | `GoldText` |
| Sell Button | `SellButton` |

#### 5.7.4 绑定 ShopUI 字段

选中 `ShopRoot`，在 **ShopUI** 组件中绑定：

| Inspector字段 | 拖入 |
|---------------|------|
| Shop Panel | `ShopPanel`（子物体） |
| Buy Panel | `BuyPanel`（挂了ShopBuyPanelUI的物体） |
| Sell Panel | `SellPanel`（挂了ShopSellPanelUI的物体） |
| Close Button | `CloseButton`（左上角返回按钮） |
| Shop Title Text | `TitleText`（标题文字） |

---

### 5.8 配置数量选择弹窗

**Hierarchy 结构**（Canvas下）:
```
QuantityDialog          ← 空物体，挂 QuantityDialogUI
└── DialogPanel         ← Image(弹窗背景，初始未激活)
    ├── ItemIcon        ← Image
    ├── ItemName        ← TextMeshPro
    ├── QuantityText    ← TextMeshPro（数量显示）
    ├── DecreaseButton  ← Button（"-"）
    ├── IncreaseButton  ← Button（"+"）
    ├── ConfirmButton   ← Button（"确认"）
    └── CancelButton    ← Button（"取消"）
```

1. Canvas下 → Create Empty → `QuantityDialog`
   - 添加组件 → **QuantityDialogUI**

2. 子物体 `DialogPanel`：居中小面板（如 300×200），**初始未激活**

3. 在 DialogPanel 下创建上述子物体

4. 绑定 QuantityDialogUI 的所有字段

---

### 5.9 配置HUD金币显示

```
CurrencyHUD         ← 空物体，挂 CurrencyUI
├── CoinIcon        ← Image（玩家金币.png）
└── GoldText        ← TextMeshPro
```

1. Canvas下 → Create Empty → `CurrencyHUD`
   - Anchor：右上角
   - 添加组件 → **CurrencyUI**

2. 子物体 CoinIcon + GoldText，水平排列

3. 绑定 **CurrencyUI** 的 `goldText` 字段

---

### 5.10 配置商店NPC

1. 选中NPC物体（或创建新的）
2. 确保有 **Collider**（如 Box Collider），勾选 **Is Trigger**
   - 调整Collider大小为交互范围（如半径3-5）
3. 添加组件 → **ShopTrigger**
4. 绑定字段：
   - **Shop Data**: 拖入5.2中创建的ShopData资产
   - **Interact Hint**: （可选）拖入交互提示UI（如"按E交互"文字，默认隐藏）

---

### 5.11 配置物品预制体的出售价格

对于需要在商店出售的物品预制体：

1. 选中物品Prefab
2. 在 **Item** 组件（或 CropItem 组件）的 Inspector 中
3. 找到 **经济属性** → **Sell Price**
4. 设置出售价格（0 = 不可出售）

> CropItem 已有独立的 sellPrice 字段，会自动使用自己的价格。

---

## 6. 操作流程

### 6.1 购买流程
```
玩家靠近NPC → 显示交互提示
  → 按E键 → ShopManager.OpenShop()
    → 商店UI显示（左侧商品 + 右侧物品栏）
    → 点击左侧商品卡片
      → 能买多个 → 弹出数量选择 → 调整数量 → 确认
      → 只能买1个 → 直接购买
        → 金币扣除 + 物品进背包
        → 背包满 → 物品掉落在NPC附近
```

### 6.2 出售流程
```
商店打开状态下
  → 点击右侧物品栏中的物品 → 槽位高亮选中
  → 点击"出售"按钮
    → 数量>1 → 弹出数量选择
    → 数量=1 → 直接出售
      → 物品移除 + 金币增加 + 物品GameObject销毁
```

### 6.3 背包查看
```
非商店状态下
  → 按B键 → 背包面板显示（40格，只读）
  → 再按B键 或 点关闭 → 背包隐藏
```

---

## 7. 事件订阅关系图

```
ShopManager ──── OnShopOpened ────────► ShopUI (显示 + 刷新)
            ──── OnShopClosed ───────► ShopUI (隐藏)
            ──── OnItemBought ───────► ShopBuyPanelUI
            ──── OnItemSold ─────────► ShopSellPanelUI (清除选中)
            ──── OnTransactionFailed ► ShopUI (提示)

CurrencyManager ─ OnGoldChanged ────► ShopSellPanelUI (更新金币)
                                    ► CurrencyUI (更新HUD)

InventoryManager ─ OnSlotChanged ──► ShopSellPanelUI (更新槽位)
                                   ► BackpackUI (更新槽位)
```

---

## 8. 注意事项

1. **脚本挂载位置**：ShopUI 和 BackpackUI 必须挂在始终激活的父物体上，面板作为子物体被控制显示/隐藏
2. **子物体命名**：ShopInventorySlotUI 通过 `transform.Find("Icon")` 和 `transform.Find("Highlight")` 查找子组件，名称必须精确匹配
3. **Player Tag**：ShopTrigger 使用 `CompareTag("Player")` 检测玩家，确保玩家物体Tag设为Player
4. **商店无限收购**：出售的物品直接Destroy，商店不存储已售物品
5. **物品栏兼容**：原有InventoryUI（快捷栏）无需任何改动，仍正常显示10个快捷栏槽位
