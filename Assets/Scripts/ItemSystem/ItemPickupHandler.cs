using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using Core.Items;
using ItemSystem.Conditions;
using ItemSystem.Effects;

namespace ItemSystem
{
    /// <summary>
    /// 物品拾取处理器 - 负责执行拾取逻辑
    /// 可扩展：通过添加条件和效果来自定义拾取行为
    /// </summary>
    public class ItemPickupHandler : MonoBehaviour
    {
    [Header("物品栏")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("当前持有的物品（兼容旧系统）")]
    public Item heldItem;
    public Item HeldItem => heldItem;

    [Header("拾取条件")]
    [Tooltip("所有条件都满足才能拾取")]
    public List<ScriptablePickupCondition> pickupConditions = new List<ScriptablePickupCondition>();

    [Header("拾取效果")]
    [Tooltip("拾取成功后依次执行的效果")]
    public List<ScriptablePickupEffect> pickupEffects = new List<ScriptablePickupEffect>();

    [Header("手部位置")]
    [Tooltip("物品附着的手部位置")]
    public Transform handPosition;

    void Start()
    {
        // 如果没有指定手部位置，尝试查找
        if (handPosition == null)
        {
            handPosition = transform.Find("HandPosition");
        }

        // 如果还是没有，创建一个
        if (handPosition == null)
        {
            var handGO = new GameObject("HandPosition");
            handGO.transform.SetParent(transform);
            handGO.transform.localPosition = new Vector3(0.5f, 1f, 0.5f);
            handPosition = handGO.transform;
        }

        // 查找物品栏管理器
        if (inventoryManager == null)
        {
            inventoryManager = GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }
        }
    }

    /// <summary>
    /// 执行拾取（核心方法）
    /// </summary>
    public bool ExecutePickup(Item item, GameObject picker)
    {
        // 1. 检查所有拾取条件
        if (!CheckAllConditions(item, picker))
        {
            return false;
        }

        // 2. 优先添加到物品栏
        if (inventoryManager != null)
        {
            if (inventoryManager.AddItem(item))
            {
                item.OnPickedUp(picker);
                Debug.Log($"{picker.name} 拾取 {item.itemName} 到物品栏");
                return true;
            }
            else
            {
                Debug.Log("物品栏已满，无法拾取");
                return false;
            }
        }

        // 3. 没有物品栏时，使用旧的手持逻辑
        item.OnPickedUp(picker);
        heldItem = item;
        ExecuteAllEffects(item, picker);

        Debug.Log($"{picker.name} picked up {item.itemName}");
        return true;
    }

    /// <summary>
    /// 检查所有拾取条件
    /// </summary>
    private bool CheckAllConditions(Item item, GameObject picker)
    {
        foreach (var condition in pickupConditions)
        {
            if (condition != null && !condition.Check(item, picker, this))
            {
                Debug.LogWarning(condition.GetFailMessage());
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 执行所有拾取效果
    /// </summary>
    private void ExecuteAllEffects(Item item, GameObject picker)
    {
        foreach (var effect in pickupEffects)
        {
            if (effect != null)
            {
                effect.Execute(item, picker, this);
            }
        }
    }

    /// <summary>
    /// 放置物品
    /// </summary>
    public void DropItem(Vector3 dropPosition)
    {
        if (heldItem == null)
        {
            Debug.Log("No item to drop");
            return;
        }

        heldItem.OnDropped(dropPosition);
        heldItem = null;
    }
}
}
