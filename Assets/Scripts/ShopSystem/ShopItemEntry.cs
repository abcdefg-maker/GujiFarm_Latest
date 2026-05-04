using UnityEngine;
using Core.Items;

namespace ShopSystem
{
    /// <summary>
    /// 商品条目数据类
    /// </summary>
    [System.Serializable]
    public class ShopItemEntry
    {
        [Tooltip("物品预制体")]
        public GameObject itemPrefab;

        [Tooltip("显示名称（空则从Prefab读取）")]
        public string displayName;

        [Tooltip("商品图标（空则从Prefab读取）")]
        public Sprite icon;

        [Tooltip("购买价格")]
        public int buyPrice = 10;

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;

            if (itemPrefab != null)
            {
                var item = itemPrefab.GetComponent<Item>();
                if (item != null) return item.itemName;
            }

            return "未知物品";
        }

        /// <summary>
        /// 获取图标
        /// </summary>
        public Sprite GetIcon()
        {
            if (icon != null) return icon;

            if (itemPrefab != null)
            {
                var item = itemPrefab.GetComponent<Item>();
                if (item != null) return item.itemIcon;
            }

            return null;
        }

        /// <summary>
        /// 获取物品ID
        /// </summary>
        public string GetItemID()
        {
            if (itemPrefab != null)
            {
                var item = itemPrefab.GetComponent<Item>();
                if (item != null) return item.itemID;
            }

            return "";
        }
    }
}
