using System;
using System.Collections;
using UnityEngine;
using Core.Items;
using Core.Constants;
using InventorySystem;
using FarmingSystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店交易逻辑管理器（单例）
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        #region Singleton
        public static ShopManager Instance { get; private set; }
        #endregion

        #region Private Fields
        private ShopData catalog;
        private Transform dropPosition;
        private InventoryManager inventoryManager;
        private CurrencyManager currencyManager;
        private bool isOpen;
        #endregion

        #region Public Properties
        public ShopData Catalog => catalog;
        public bool IsOpen => isOpen;
        #endregion

        #region Events
        public event Action OnShopOpened;
        public event Action OnShopClosed;
        public event Action<ShopItemEntry, int> OnItemBought;
        public event Action<int, int> OnItemSold;
        public event Action<string> OnTransactionFailed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            inventoryManager = InventoryManager.Instance;
            currencyManager = CurrencyManager.Instance;

            if (inventoryManager == null)
                Debug.LogError("[ShopManager] 找不到 InventoryManager!");
            if (currencyManager == null)
                Debug.LogError("[ShopManager] 找不到 CurrencyManager!");
        }
        #endregion

        #region Shop Open/Close
        /// <summary>
        /// 打开商店
        /// </summary>
        public void OpenShop(ShopData shopData, Transform shopTransform)
        {
            if (isOpen) return;
            if (shopData == null) return;

            catalog = shopData;
            dropPosition = shopTransform;
            isOpen = true;

            OnShopOpened?.Invoke();
            Debug.Log($"[ShopManager] 打开商店: {catalog.shopName}");
        }

        /// <summary>
        /// 关闭商店
        /// </summary>
        public void CloseShop()
        {
            if (!isOpen) return;

            isOpen = false;
            OnShopClosed?.Invoke();
            Debug.Log("[ShopManager] 关闭商店");
        }
        #endregion

        #region Buy
        /// <summary>
        /// 购买物品
        /// </summary>
        public bool BuyItem(ShopItemEntry entry, int amount)
        {
            if (entry == null || amount <= 0) return false;
            if (entry.itemPrefab == null)
            {
                OnTransactionFailed?.Invoke("商品无效");
                return false;
            }

            int totalCost = entry.buyPrice * amount;

            if (!currencyManager.SpendGold(totalCost))
            {
                OnTransactionFailed?.Invoke("金币不足");
                return false;
            }

            for (int i = 0; i < amount; i++)
            {
                GameObject obj = Instantiate(entry.itemPrefab);
                Item item = obj.GetComponent<Item>();

                if (item == null)
                {
                    Destroy(obj);
                    continue;
                }

                if (!inventoryManager.AddItem(item))
                {
                    // 背包满，掉落在商店附近
                    DropItemNearShop(item);
                }
            }

            OnItemBought?.Invoke(entry, amount);
            Debug.Log($"[ShopManager] 购买 {entry.GetDisplayName()} x{amount}，花费 {totalCost} 金币");
            return true;
        }

        private void DropItemNearShop(Item item)
        {
            if (dropPosition == null)
            {
                item.OnDropped(item.transform.position);
                return;
            }

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * GameConstants.ShopDropScatterRadius;
            Vector3 dropPos = dropPosition.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            item.gameObject.SetActive(true);
            item.OnDropped(dropPos);
            Debug.Log($"[ShopManager] 背包已满，{item.itemName} 掉落在商店附近");
        }
        #endregion

        #region Sell
        /// <summary>
        /// 出售物品（商店无限收购，物品直接销毁）
        /// </summary>
        public bool SellItem(int slotIndex, int amount)
        {
            if (amount <= 0) return false;

            InventorySlot slot = inventoryManager.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) return false;
            if (slot.count < amount) return false;

            if (catalog != null && !catalog.CanSellItemType(slot.ItemType))
            {
                OnTransactionFailed?.Invoke("该物品不可出售");
                return false;
            }

            int unitPrice = GetSellPrice(slot.itemRef);
            if (unitPrice <= 0)
            {
                OnTransactionFailed?.Invoke("该物品不可出售");
                return false;
            }

            int totalIncome = unitPrice * amount;

            Item item = inventoryManager.RemoveItem(slotIndex, amount);
            currencyManager.AddGold(totalIncome);

            // 商店无限收购：直接销毁物品
            if (item != null && slot.IsEmpty)
            {
                Destroy(item.gameObject);
            }

            OnItemSold?.Invoke(slotIndex, amount);
            Debug.Log($"[ShopManager] 出售 x{amount}，获得 {totalIncome} 金币");
            return true;
        }

        /// <summary>
        /// 获取物品的出售价格
        /// </summary>
        public int GetSellPrice(Item item)
        {
            if (item == null) return 0;

            int basePrice;

            if (item is CropItem cropItem)
            {
                basePrice = cropItem.SellPrice;
            }
            else
            {
                basePrice = item.sellPrice;
            }

            if (basePrice <= 0) return 0;

            float multiplier = catalog != null ? catalog.sellPriceMultiplier : 1f;
            return Mathf.RoundToInt(basePrice * multiplier);
        }
        #endregion
    }
}
