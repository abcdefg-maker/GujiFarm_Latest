using System.Collections.Generic;
using UnityEngine;
using Core.Items;

namespace ItemSystem
{
    /// <summary>
    /// 物品交互器 - 负责检测和选择物品
    /// 可复用：任何GameObject都能添加此组件来获得拾取能力
    /// </summary>
    public class ItemInteractor : MonoBehaviour
    {
    [Header("检测设置")]
    [Tooltip("检测范围半径")]
    public float detectionRadius = 2f;

    [Tooltip("检测中心偏移（相对于自身位置）")]
    public Vector3 detectionOffset = Vector3.zero;

    [Tooltip("物品所在的Layer")]
    public LayerMask itemLayer;

    [Tooltip("检测方式")]
    public DetectionMode detectionMode = DetectionMode.OverlapSphere;

    [Header("物品选择")]
    [Tooltip("当多个物品在范围内时的选择策略")]
    public SelectionStrategy selectionStrategy = SelectionStrategy.Nearest;

    [Header("调试")]
    public bool showDebugGizmos = true;

    // 附近的物品列表
    private List<Item> nearbyItems = new List<Item>();

    // 当前目标物品
    private Item targetItem;
    public Item TargetItem => targetItem;

    // Trigger检测用（如果使用Trigger模式）
    private HashSet<Item> triggeredItems = new HashSet<Item>();

    void Update()
    {
        UpdateNearbyItems();
        UpdateTargetItem();
    }

    /// <summary>
    /// 更新附近的物品列表
    /// </summary>
    private void UpdateNearbyItems()
    {
        nearbyItems.Clear();

        switch (detectionMode)
        {
            case DetectionMode.OverlapSphere:
                DetectByOverlapSphere();
                break;

            case DetectionMode.Trigger:
                DetectByTrigger();
                break;
        }
    }

    /// <summary>
    /// 使用物理检测
    /// </summary>
    private void DetectByOverlapSphere()
    {
        Vector3 detectionCenter = transform.position + detectionOffset;
        Collider[] colliders = Physics.OverlapSphere(
            detectionCenter,
            detectionRadius,
            itemLayer
        );

        foreach (var col in colliders)
        {
            var item = col.GetComponent<Item>();
            if (item != null && item.canBePickedUp && !item.IsPickedUp)
            {
                nearbyItems.Add(item);
            }
        }
    }

    /// <summary>
    /// 使用Trigger检测
    /// </summary>
    private void DetectByTrigger()
    {
        Vector3 detectionCenter = transform.position + detectionOffset;
        foreach (var item in triggeredItems)
        {
            // 过滤掉已被拾取的物品，并检查距离
            if (item != null && item.canBePickedUp && !item.IsPickedUp)
            {
                float distance = Vector3.Distance(detectionCenter, item.transform.position);
                if (distance <= detectionRadius)
                {
                    nearbyItems.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// 更新目标物品
    /// </summary>
    private void UpdateTargetItem()
    {
        if (nearbyItems.Count == 0)
        {
            targetItem = null;
            return;
        }

        targetItem = SelectTargetItem();
    }

    /// <summary>
    /// 根据策略选择目标物品
    /// </summary>
    private Item SelectTargetItem()
    {
        switch (selectionStrategy)
        {
            case SelectionStrategy.Nearest:
                return GetNearestItem();

            case SelectionStrategy.First:
                return nearbyItems[0];

            default:
                return nearbyItems[0];
        }
    }

    /// <summary>
    /// 获取最近的物品
    /// </summary>
    private Item GetNearestItem()
    {
        Vector3 detectionCenter = transform.position + detectionOffset;
        Item nearest = nearbyItems[0];
        float minDist = Vector3.Distance(detectionCenter, nearest.transform.position);

        for (int i = 1; i < nearbyItems.Count; i++)
        {
            float dist = Vector3.Distance(detectionCenter, nearbyItems[i].transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = nearbyItems[i];
            }
        }

        return nearest;
    }

    /// <summary>
    /// 尝试拾取目标物品（对外接口）
    /// </summary>
    public bool TryPickupItem()
    {
        if (targetItem == null)
        {
            Debug.Log("No item to pick up");
            return false;
        }

        // 调用拾取处理器
        var handler = GetComponent<ItemPickupHandler>();
        if (handler != null)
        {
            return handler.ExecutePickup(targetItem, gameObject);
        }
        else
        {
            Debug.LogWarning("No ItemPickupHandler found!");
            return false;
        }
    }

    /// <summary>
    /// 检查是否有可拾取的物品
    /// </summary>
    public bool HasItemInRange()
    {
        return targetItem != null;
    }

    // Trigger事件处理
    private void OnTriggerEnter(Collider other)
    {
        if (detectionMode != DetectionMode.Trigger) return;

        var item = other.GetComponent<Item>();
        if (item != null && item.canBePickedUp && !item.IsPickedUp)
        {
            triggeredItems.Add(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (detectionMode != DetectionMode.Trigger) return;

        var item = other.GetComponent<Item>();
        if (item != null)
        {
            triggeredItems.Remove(item);
        }
    }

    // 调试绘制
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 绘制检测范围
        Vector3 detectionCenter = transform.position + detectionOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionCenter, detectionRadius);

        // 绘制目标物品连线
        if (targetItem != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetItem.transform.position);
        }
    }

    public enum DetectionMode
    {
        OverlapSphere,  // 使用Physics.OverlapSphere
        Trigger         // 使用OnTriggerEnter/Exit
    }

    public enum SelectionStrategy
    {
        Nearest,        // 最近的物品
        First           // 第一个检测到的
    }
}
}
