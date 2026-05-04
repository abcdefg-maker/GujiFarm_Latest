using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShopSystem
{
    /// <summary>
    /// 左侧购买面板 — 生成商品卡片网格
    /// </summary>
    public class ShopBuyPanelUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("引用")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private GameObject slotPrefab;
        #endregion

        #region Private Fields
        private ShopManager shopManager;
        private CurrencyManager currencyManager;
        private List<ShopItemSlotUI> slotUIs = new List<ShopItemSlotUI>();
        private bool initialized;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            shopManager = ShopManager.Instance;
            currencyManager = CurrencyManager.Instance;

            if (shopManager == null)
            {
                Debug.LogError("[ShopBuyPanelUI] 找不到 ShopManager!");
                yield break;
            }

            shopManager.OnItemBought += OnItemBought;
            initialized = true;
        }

        private void OnDestroy()
        {
            if (shopManager != null)
            {
                shopManager.OnItemBought -= OnItemBought;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 刷新商品列表
        /// </summary>
        public void RefreshUI()
        {
            // 延迟初始化：首次打开商店时 DelayedInit 可能尚未完成
            if (shopManager == null)
                shopManager = ShopManager.Instance;
            if (currencyManager == null)
                currencyManager = CurrencyManager.Instance;

            if (shopManager == null || shopManager.Catalog == null) return;

            ClearSlots();

            var items = shopManager.Catalog.items;
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null || items[i].itemPrefab == null) continue;

                GameObject slotObj = Instantiate(slotPrefab, gridParent);
                ShopItemSlotUI slotUI = slotObj.GetComponent<ShopItemSlotUI>();

                // 预制体缺少 ShopItemSlotUI / Button 时自动补充
                if (slotUI == null)
                    slotUI = slotObj.AddComponent<ShopItemSlotUI>();
                if (slotObj.GetComponent<UnityEngine.UI.Button>() == null)
                    slotObj.AddComponent<UnityEngine.UI.Button>();

                slotUI.Initialize();
                slotUI.Setup(items[i]);
                slotUI.OnBuyClicked += HandleBuyClicked;
                slotUIs.Add(slotUI);
            }
        }
        #endregion

        #region Private Methods
        private void HandleBuyClicked(ShopItemEntry entry)
        {
            if (entry == null || currencyManager == null) return;

            int maxCanBuy = entry.buyPrice > 0 ? currencyManager.CurrentGold / entry.buyPrice : 0;

            if (maxCanBuy <= 0)
            {
                shopManager.BuyItem(entry, 1); // 会触发"金币不足"提示
                return;
            }

            if (maxCanBuy == 1)
            {
                shopManager.BuyItem(entry, 1);
            }
            else
            {
                // 打开数量选择弹窗
                QuantityDialogUI.Show(
                    entry.GetDisplayName(),
                    entry.GetIcon(),
                    1,
                    maxCanBuy,
                    (quantity) => shopManager.BuyItem(entry, quantity)
                );
            }
        }

        private void OnItemBought(ShopItemEntry entry, int amount)
        {
            // 购买后刷新（更新可购买状态等）
        }

        private void ClearSlots()
        {
            // 先取消事件订阅
            foreach (var slotUI in slotUIs)
            {
                if (slotUI != null)
                    slotUI.OnBuyClicked -= HandleBuyClicked;
            }
            slotUIs.Clear();

            // 销毁 gridParent 下所有子物体（防止未追踪的残留槽位堆积）
            if (gridParent != null)
            {
                for (int i = gridParent.childCount - 1; i >= 0; i--)
                {
                    GameObject child = gridParent.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
            }
        }
        #endregion
    }
}
