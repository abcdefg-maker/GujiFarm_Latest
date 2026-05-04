using UnityEngine;
using UnityEngine.EventSystems;
using ShopSystem;

namespace InventorySystem
{
    /// <summary>
    /// 可拖拽槽位组件 — 附加到使用ShopInventorySlotUI的槽位上
    /// 实现物品栏槽位之间的拖拽交换
    ///
    /// 此组件通过代码动态添加（AddComponent），不放入Prefab，
    /// 以避免商店面板的槽位意外获得拖拽功能。
    /// </summary>
    public class DraggableSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private int slotIndex;
        private InventoryManager inventoryManager;

        /// <summary>
        /// 初始化（由宿主UI调用）
        /// </summary>
        public void Initialize(int index)
        {
            slotIndex = index;
            inventoryManager = InventoryManager.Instance;
        }

        public int SlotIndex => slotIndex;

        #region IBeginDragHandler
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 商店打开时禁止拖拽
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
            {
                eventData.pointerDrag = null;
                return;
            }

            if (inventoryManager == null) return;

            InventorySlot slot = inventoryManager.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                // 空槽位不可拖起
                eventData.pointerDrag = null;
                return;
            }

            // 显示幽灵图标
            if (DragGhostUI.Instance != null)
            {
                Sprite icon = slot.itemRef != null ? slot.itemRef.itemIcon : null;
                DragGhostUI.Instance.Show(icon, slot.count);
            }
        }
        #endregion

        #region IDragHandler
        public void OnDrag(PointerEventData eventData)
        {
            if (DragGhostUI.Instance != null)
            {
                DragGhostUI.Instance.FollowPointer(eventData);
            }
        }
        #endregion

        #region IEndDragHandler
        public void OnEndDrag(PointerEventData eventData)
        {
            // 无论结果如何都隐藏幽灵
            if (DragGhostUI.Instance != null)
            {
                DragGhostUI.Instance.Hide();
            }
        }
        #endregion

        #region IDropHandler
        public void OnDrop(PointerEventData eventData)
        {
            if (inventoryManager == null) return;

            // 从拖拽数据获取源槽位
            GameObject draggedObj = eventData.pointerDrag;
            if (draggedObj == null) return;

            DraggableSlotUI sourceSlot = draggedObj.GetComponent<DraggableSlotUI>();
            if (sourceSlot == null) return;

            // 交换两个槽位
            inventoryManager.SwapSlots(sourceSlot.SlotIndex, slotIndex);
        }
        #endregion
    }
}
