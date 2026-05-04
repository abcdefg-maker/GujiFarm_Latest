# Farming System Implementation Plan

> **Project**: GuJi_Farm_Project
> **Unity Version**: 2022.3.62f3c1 (LTS)
> **Author**: Claude AI
> **Date**: 2026-02-05
> **Version**: v1.0
> **Status**: Planning

---

## 1. Overview

Based on the existing Time System and Tool System, implement a complete farming and planting system.

---

## 2. Core Requirements

| # | Requirement | Description |
|---|-------------|-------------|
| 1 | Farm Plot | Area that can be interacted with using a hoe |
| 2 | Soil Mound Generation | Hoe creates soil mounds, minimum distance between mounds ≥ 0.5x mound size |
| 3 | Planting System | Plant seeds on soil mounds |
| 4 | Growth System | 5 growth stages, 1 stage per day (configurable) |
| 5 | Watering Boost | Watering skips 1 stage, has cooldown (default 1 real second) |
| 6 | Crop Base Class | Extensible base class for different crop types |
| 7 | Harvest System | Sickle harvests mature crops, soil mound destroyed after harvest |

---

## 3. File Structure

```
Assets/Scripts/FarmingSystem/
├── GrowthStage.cs           # Growth stage enum
├── CropData.cs              # Crop configuration (ScriptableObject)
├── FarmPlot.cs              # Farm plot (accepts hoe)
├── SoilMound.cs             # Soil mound (planting carrier)
├── CropBase.cs              # Crop base class
├── SeedItem.cs              # Seed item
└── CropItem.cs              # Harvested crop item
```

---

## 4. Detailed Design

### 4.1 GrowthStage Enum

```csharp
public enum GrowthStage
{
    Seed = 0,           // Just planted
    Sprout = 1,         // Sprouting
    Growing = 2,        // Growing
    Mature = 3,         // Almost mature
    Harvestable = 4     // Ready to harvest
}
```

---

### 4.2 CropData (ScriptableObject)

**Purpose**: Define all configurable properties for crops

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| cropID | string | - | Unique identifier |
| cropName | string | - | Display name |
| daysPerStage | int | 1 | Days needed per growth stage |
| waterCooldown | float | 1f | Watering cooldown (real seconds) |
| waterSpeedUpStages | int | 1 | Stages skipped when watered |
| stageSprites | Sprite[5] | - | Sprites for each growth stage |
| harvestItemPrefab | GameObject | - | Prefab for harvested item |
| minHarvestAmount | int | 1 | Minimum harvest yield |
| maxHarvestAmount | int | 3 | Maximum harvest yield |
| seedItemPrefab | GameObject | - | Reference to seed prefab |

**Editor Menu**: `Create → Farming → Crop Data`

---

### 4.3 FarmPlot

**Purpose**: Area that accepts hoe interaction and generates soil mounds

**Implements**: `IToolTarget` interface

| Property | Type | Description |
|----------|------|-------------|
| soilMoundPrefab | GameObject | Soil mound prefab |
| minMoundDistance | float | Minimum distance between mounds (multiplier of mound size) |
| moundSize | float | Actual mound size |
| existingMounds | List\<SoilMound\> | List of existing mounds |

**Key Methods**:

```
CanInteract(tool) → true if tool is Hoe
ReceiveToolAction(tool, user) → Create soil mound at hit point
IsTooCloseToExistingMound(position) → Check distance constraint
CreateSoilMound(position) → Instantiate and register mound
```

**Distance Check Formula**:
```
minAllowedDistance = moundSize × (1 + minMoundDistance)
// With moundSize=1 and minMoundDistance=0.5, minimum distance is 1.5 units
```

---

### 4.4 SoilMound

**Purpose**: Planting carrier, manages crop lifecycle

**Implements**: `IToolTarget` interface

| Property | Type | Description |
|----------|------|-------------|
| parentPlot | FarmPlot | Parent farm plot |
| currentCrop | CropBase | Currently planted crop |
| soilRenderer | SpriteRenderer | Soil visual |
| cropAnchor | Transform | Position for crop sprite |

**Accepted Tools by State**:

| State | Accepted Tools |
|-------|----------------|
| Empty (no crop) | None |
| Growing crop | WateringCan |
| Harvestable crop | Sickle |

**Key Methods**:

```
Plant(seed) → Create crop from seed data
Water() → Call crop.Water()
Harvest(user) → Call crop.Harvest(), destroy self
OnMouseDown() → Handle seed planting via mouse click
```

**Planting Interaction**:
- Player selects seed in inventory
- Player left-clicks on empty soil mound
- Seed is consumed, crop is created

---

### 4.5 CropBase

**Purpose**: Core crop logic, extensible base class

| Property | Type | Description |
|----------|------|-------------|
| cropData | CropData | Configuration reference |
| currentStage | GrowthStage | Current growth stage |
| daysInCurrentStage | int | Days spent in current stage |
| waterCooldownTimer | float | Remaining cooldown time |
| canBeWatered | bool | Whether watering is allowed |
| spriteRenderer | SpriteRenderer | Visual component |
| parentMound | SoilMound | Parent soil mound |

**Time System Integration**:
```csharp
// Subscribe in Start()
TimeManager.Instance.OnDayChanged += OnDayPassed;

// Unsubscribe in OnDestroy()
TimeManager.Instance.OnDayChanged -= OnDayPassed;
```

**Growth Logic**:
```
OnDayPassed(day):
    if stage == Harvestable: return
    daysInCurrentStage++
    if daysInCurrentStage >= cropData.daysPerStage:
        AdvanceStage()

AdvanceStage():
    if stage < Harvestable:
        stage++
        daysInCurrentStage = 0
        UpdateVisual()
```

**Watering Logic**:
```
Water():
    if !canBeWatered: return
    if stage == Harvestable: return

    for i in range(waterSpeedUpStages):
        if stage < Harvestable:
            AdvanceStage()

    canBeWatered = false
    waterCooldownTimer = cropData.waterCooldown

Update():
    if !canBeWatered:
        waterCooldownTimer -= Time.deltaTime
        if waterCooldownTimer <= 0:
            canBeWatered = true
```

**Harvest Logic**:
```
Harvest():
    if !IsHarvestable: return

    amount = Random.Range(minAmount, maxAmount + 1)
    for i in range(amount):
        Instantiate(harvestItemPrefab, randomOffset)
```

---

### 4.6 SeedItem

**Purpose**: Plantable seed item

**Inherits**: `Item`

| Property | Type | Description |
|----------|------|-------------|
| cropData | CropData | Reference to crop configuration |

```csharp
// In Awake()
itemType = ItemType.Seed;

// Plant method
PlantTo(mound):
    if mound.Plant(this):
        Destroy(gameObject)  // Consume seed
```

---

### 4.7 CropItem

**Purpose**: Harvested crop item

**Inherits**: `Item`

| Property | Type | Description |
|----------|------|-------------|
| cropData | CropData | Reference to crop configuration |

```csharp
// In Awake()
itemType = ItemType.Crop;
```

---

## 5. Interaction Flow

### 5.1 Complete Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    FARMING SYSTEM FLOW                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. TILLING (Create Soil Mound)                                │
│     Player + Hoe + Left Click on FarmPlot                      │
│         ↓                                                       │
│     FarmPlot.ReceiveToolAction()                               │
│         ↓                                                       │
│     Check distance constraint                                   │
│         ↓                                                       │
│     CreateSoilMound() → New SoilMound instance                 │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  2. PLANTING (Plant Seed)                                      │
│     Player selects Seed in inventory                           │
│         ↓                                                       │
│     Left Click on empty SoilMound                              │
│         ↓                                                       │
│     SoilMound.Plant(seed)                                      │
│         ↓                                                       │
│     Create CropBase instance                                    │
│         ↓                                                       │
│     Remove seed from inventory                                  │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  3. GROWTH (Automatic)                                         │
│     TimeManager.OnDayChanged event                             │
│         ↓                                                       │
│     CropBase.OnDayPassed()                                     │
│         ↓                                                       │
│     daysInCurrentStage++                                       │
│         ↓                                                       │
│     If days >= daysPerStage → AdvanceStage()                   │
│         ↓                                                       │
│     UpdateVisual() → Change sprite                             │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  4. WATERING (Speed Up Growth)                                 │
│     Player + WateringCan + Left Click on SoilMound             │
│         ↓                                                       │
│     SoilMound.ReceiveToolAction()                              │
│         ↓                                                       │
│     CropBase.Water()                                           │
│         ↓                                                       │
│     Skip 1 stage (configurable)                                │
│         ↓                                                       │
│     Enter cooldown (1 second default)                          │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  5. HARVEST (Collect Crop)                                     │
│     Player + Sickle + Left Click on mature SoilMound           │
│         ↓                                                       │
│     SoilMound.ReceiveToolAction()                              │
│         ↓                                                       │
│     CropBase.Harvest() → Spawn CropItem(s)                     │
│         ↓                                                       │
│     Destroy SoilMound                                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 State Diagram

```
                    ┌─────────────┐
                    │  FarmPlot   │
                    │  (Empty)    │
                    └──────┬──────┘
                           │ Hoe
                           ▼
                    ┌─────────────┐
                    │ SoilMound   │
                    │  (Empty)    │
                    └──────┬──────┘
                           │ Seed + Click
                           ▼
┌──────────────────────────────────────────────────────┐
│                    CropBase                          │
│                                                      │
│   Seed ──→ Sprout ──→ Growing ──→ Mature ──→ Harvestable │
│    │         │          │          │           │     │
│    └─────────┴──────────┴──────────┘           │     │
│         (WateringCan speeds up)                │     │
│                                                │     │
└────────────────────────────────────────────────┼─────┘
                                                 │ Sickle
                                                 ▼
                                          ┌─────────────┐
                                          │  CropItem   │
                                          │ (Spawned)   │
                                          └─────────────┘
                                                 +
                                          ┌─────────────┐
                                          │ SoilMound   │
                                          │ (Destroyed) │
                                          └─────────────┘
```

---

## 6. Integration with Existing Systems

### 6.1 Tool System Integration

| Tool | Target | Action |
|------|--------|--------|
| Hoe | FarmPlot | Create SoilMound |
| WateringCan | SoilMound (with growing crop) | Speed up growth |
| Sickle | SoilMound (with mature crop) | Harvest and destroy |

### 6.2 Time System Integration

```csharp
// CropBase subscribes to day change event
TimeManager.Instance.OnDayChanged += OnDayPassed;
```

### 6.3 Inventory System Integration

```csharp
// When planting
var inventory = InventoryManager.Instance;
var selectedItem = inventory.GetSelectedItem();
if (selectedItem is SeedItem seed)
{
    if (mound.Plant(seed))
    {
        inventory.RemoveItem(inventory.SelectedIndex, 1);
    }
}
```

---

## 7. Configurable Parameters

| Parameter | Location | Default | Description |
|-----------|----------|---------|-------------|
| daysPerStage | CropData | 1 | Days per growth stage |
| waterCooldown | CropData | 1.0s | Watering cooldown (real seconds) |
| waterSpeedUpStages | CropData | 1 | Stages skipped when watered |
| minMoundDistance | FarmPlot | 0.5 | Minimum mound spacing (multiplier) |
| moundSize | FarmPlot | 1.0 | Mound size in units |
| minHarvestAmount | CropData | 1 | Minimum harvest yield |
| maxHarvestAmount | CropData | 3 | Maximum harvest yield |

---

## 8. Implementation Steps

### Phase 1: Basic Files
- [ ] Create `FarmingSystem` folder
- [ ] Create `GrowthStage.cs` enum
- [ ] Create `CropData.cs` ScriptableObject

### Phase 2: Core Classes
- [ ] Create `FarmPlot.cs` (implements IToolTarget)
- [ ] Create `SoilMound.cs` (implements IToolTarget)
- [ ] Create `CropBase.cs` (subscribes to TimeManager)

### Phase 3: Item Classes
- [ ] Create `SeedItem.cs`
- [ ] Create `CropItem.cs`

### Phase 4: Testing Resources
- [ ] Create test CropData asset
- [ ] Create SoilMound prefab
- [ ] Create Seed and Crop prefabs

---

## 9. Testing Plan

| Test | Steps | Expected Result |
|------|-------|-----------------|
| Tilling | Hoe + click FarmPlot | SoilMound created |
| Distance Check | Create mounds close together | Second mound rejected if too close |
| Planting | Select seed + click SoilMound | Seed consumed, crop appears |
| Growth | Call `TimeManager.SkipToNextDay()` | Crop advances stage |
| Watering | WateringCan + click crop | Stage skipped, cooldown starts |
| Cooldown | Water twice quickly | Second water rejected |
| Harvest | Sickle + click mature crop | Items spawned, mound destroyed |

---

## 10. Future Extensions

- [ ] Different crop types with unique behaviors
- [ ] Seasonal crop restrictions
- [ ] Crop quality system
- [ ] Fertilizer items
- [ ] Pest/disease system
- [ ] Automated watering (sprinklers)

---

## 11. File Dependencies

```
FarmingSystem/
├── GrowthStage.cs      (no dependencies)
├── CropData.cs         (depends on: GrowthStage)
├── CropBase.cs         (depends on: CropData, GrowthStage, TimeManager)
├── FarmPlot.cs         (depends on: SoilMound, IToolTarget)
├── SoilMound.cs        (depends on: CropBase, FarmPlot, IToolTarget)
├── SeedItem.cs         (depends on: Item, CropData)
└── CropItem.cs         (depends on: Item, CropData)

External Dependencies:
├── TimeSystem/TimeManager.cs
├── ToolSystem/IToolTarget.cs
├── ToolSystem/ToolType.cs
├── InventorySystem/InventoryManager.cs
└── Item.cs
```

---

**Document End**
