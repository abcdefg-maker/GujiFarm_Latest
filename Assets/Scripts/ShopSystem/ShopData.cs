using UnityEngine;
using Core.Items;

namespace ShopSystem
{
    /// <summary>
    /// 商店配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "Shop/Shop Data", fileName = "NewShopData")]
    public class ShopData : ScriptableObject
    {
        [Header("商店信息")]
        [Tooltip("商店名称")]
        public string shopName = "神秘商店";

        [Header("商品列表")]
        [Tooltip("可购买的商品")]
        public ShopItemEntry[] items;

        [Header("出售设置")]
        [Tooltip("可出售的物品类型")]
        public ItemType[] acceptedSellTypes = new ItemType[]
        {
            ItemType.Crop,
            ItemType.Material,
            ItemType.Seed
        };

        [Tooltip("出售价格倍率")]
        [Range(0.1f, 2f)]
        public float sellPriceMultiplier = 1f;

        /// <summary>
        /// 检查是否接受该类型的物品出售
        /// </summary>
        public bool CanSellItemType(ItemType type)
        {
            if (acceptedSellTypes == null) return false;

            for (int i = 0; i < acceptedSellTypes.Length; i++)
            {
                if (acceptedSellTypes[i] == type) return true;
            }

            return false;
        }
    }
}
