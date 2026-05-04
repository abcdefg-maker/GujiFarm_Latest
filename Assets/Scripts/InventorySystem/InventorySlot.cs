using System.Collections.Generic;
using UnityEngine;
using Core.Items;

namespace InventorySystem
{
    /// <summary>
    /// 物品栏槽位数据
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [Header("物品引用")]
        public Item itemRef;            // 显示用的物品引用（代表此槽位的物品类型）

        [Header("堆叠信息")]
        public int count = 0;           // 当前数量
        public int maxStack = 99;       // 最大堆叠数

        // 堆叠的所有物品实例（包括itemRef自身）
        private List<Item> stackedItems = new List<Item>();

        /// <summary>
        /// 槽位是否为空
        /// </summary>
        public bool IsEmpty => itemRef == null || count <= 0;

        /// <summary>
        /// 槽位是否已满
        /// </summary>
        public bool IsFull => count >= maxStack;

        /// <summary>
        /// 物品ID（快捷访问）
        /// </summary>
        public string ItemID => itemRef != null ? itemRef.itemID : "";

        /// <summary>
        /// 物品类型（快捷访问）
        /// </summary>
        public ItemType ItemType => itemRef != null ? itemRef.itemType : ItemType.Material;

        /// <summary>
        /// 清空槽位
        /// </summary>
        public void Clear()
        {
            itemRef = null;
            count = 0;
            stackedItems.Clear();
        }

        /// <summary>
        /// 设置物品（首次放入空槽位）
        /// </summary>
        public void SetItem(Item item, int amount = 1)
        {
            itemRef = item;
            count = amount;

            stackedItems.Clear();
            if (item != null)
            {
                stackedItems.Add(item);
            }

            // 工具不可堆叠
            if (item != null && item.itemType == ItemType.Tool)
            {
                maxStack = 1;
            }
            else
            {
                maxStack = 99;
            }
        }

        /// <summary>
        /// 检查是否可以与指定物品堆叠
        /// </summary>
        public bool CanStackWith(Item item)
        {
            if (IsEmpty) return false;
            if (IsFull) return false;
            if (item == null) return false;

            // 工具不可堆叠
            if (item.itemType == ItemType.Tool) return false;

            // 相同物品ID才可堆叠
            return itemRef.itemID == item.itemID;
        }

        /// <summary>
        /// 堆叠一个物品实例（增加数量并记录引用）
        /// </summary>
        public int StackItem(Item item)
        {
            if (item == null) return 0;
            int canAdd = Mathf.Min(1, maxStack - count);
            if (canAdd <= 0) return 0;

            count += canAdd;
            stackedItems.Add(item);
            return canAdd;
        }

        /// <summary>
        /// 取出一个物品实例（减少数量并返回实际的GameObject引用）
        /// </summary>
        public Item PopItem()
        {
            if (stackedItems.Count == 0) return null;

            // 从末尾取出（后进先出）
            Item item = stackedItems[stackedItems.Count - 1];
            stackedItems.RemoveAt(stackedItems.Count - 1);
            count--;

            if (count <= 0 || stackedItems.Count == 0)
            {
                Clear();
            }
            else
            {
                // 更新 itemRef 为栈顶
                itemRef = stackedItems[0];
            }

            return item;
        }

        /// <summary>
        /// 增加数量（不带物品引用，仅用于数值调整）
        /// </summary>
        /// <returns>实际增加的数量</returns>
        public int AddCount(int amount)
        {
            int canAdd = Mathf.Min(amount, maxStack - count);
            count += canAdd;
            return canAdd;
        }

        /// <summary>
        /// 减少数量
        /// </summary>
        /// <returns>实际减少的数量</returns>
        public int RemoveCount(int amount)
        {
            int canRemove = Mathf.Min(amount, count);
            count -= canRemove;

            // 同步清理 stackedItems（从末尾移除）
            for (int i = 0; i < canRemove && stackedItems.Count > 0; i++)
            {
                stackedItems.RemoveAt(stackedItems.Count - 1);
            }

            if (count <= 0)
            {
                Clear();
            }

            return canRemove;
        }
    }
}
