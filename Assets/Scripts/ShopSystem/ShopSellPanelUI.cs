using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Constants;
using Core.Items;
using InventorySystem;

namespace ShopSystem
{
    /// <summary>
    /// 右侧出售面板 — 双网格物品栏 + 金币显示 + 出售按钮
    /// 上方: 背包网格(8×5=40格, 槽位10-49)
    /// 下方: 快捷栏网格(10格, 槽位0-9)
    /// </summary>
    public class ShopSellPanelUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("网格容器")]
        [SerializeField] private Transform backpackGridParent;
        [SerializeField] private Transform hotbarGridParent;
        [SerializeField] private GameObject slotPrefab;

        [Header("底部栏")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button sellButton;
        #endregion

        #region Private Fields
        private InventoryManager inventoryManager;
        private CurrencyManager currencyManager;
        private ShopManager shopManager;
        private ShopInventorySlotUI[] slotUIs;
        private int selectedSlotIndex = -1;
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

            inventoryManager = InventoryManager.Instance;
            currencyManager = CurrencyManager.Instance;
            shopManager = ShopManager.Instance;

            if (inventoryManager == null)
            {
                Debug.LogError("[ShopSellPanelUI] 找不到 InventoryManager!");
                yield break;
            }

            if (slotUIs == null)
            {
                InitializeGrids();
                SubscribeEvents();
            }

            initialized = true;
        }

        /// <summary>
        /// 订阅事件（仅调用一次）
        /// </summary>
        private void SubscribeEvents()
        {
            if (inventoryManager != null)
                inventoryManager.OnSlotChanged += OnSlotChanged;

            if (currencyManager != null)
                currencyManager.OnGoldChanged += OnGoldChanged;

            if (sellButton != null)
                sellButton.onClick.AddListener(OnSellButtonClicked);

            if (shopManager != null)
                shopManager.OnItemSold += OnItemSold;
        }

        private void OnDestroy()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnSlotChanged -= OnSlotChanged;
            }

            if (currencyManager != null)
            {
                currencyManager.OnGoldChanged -= OnGoldChanged;
            }

            if (sellButton != null)
            {
                sellButton.onClick.RemoveListener(OnSellButtonClicked);
            }

            if (shopManager != null)
            {
                shopManager.OnItemSold -= OnItemSold;
            }
        }
        #endregion

        #region Initialization
        private void InitializeGrids()
        {
            int totalSlots = inventoryManager.MaxSlots;
            int hotbarSize = inventoryManager.HotbarSize;
            int backpackStart = inventoryManager.BackpackStartIndex;

            slotUIs = new ShopInventorySlotUI[totalSlots];

            // 快捷栏: 槽位0-9 → hotbarGridParent
            if (hotbarGridParent != null && slotPrefab != null)
            {
                for (int i = 0; i < hotbarSize; i++)
                {
                    GameObject slotObj = Instantiate(slotPrefab, hotbarGridParent);
                    ShopInventorySlotUI slotUI = slotObj.GetComponent<ShopInventorySlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.Initialize(i);
                        slotUI.OnSlotClicked += OnInventorySlotClicked;
                        slotUIs[i] = slotUI;
                    }
                }
            }

            // 背包: 槽位10-49 → backpackGridParent
            if (backpackGridParent != null && slotPrefab != null)
            {
                for (int i = backpackStart; i < totalSlots; i++)
                {
                    GameObject slotObj = Instantiate(slotPrefab, backpackGridParent);
                    ShopInventorySlotUI slotUI = slotObj.GetComponent<ShopInventorySlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.Initialize(i);
                        slotUI.OnSlotClicked += OnInventorySlotClicked;
                        slotUIs[i] = slotUI;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 刷新所有槽位和金币显示
        /// </summary>
        public void RefreshUI()
        {
            // 延迟初始化：首次打开商店时 DelayedInit 可能尚未完成
            if (inventoryManager == null)
                inventoryManager = InventoryManager.Instance;
            if (currencyManager == null)
                currencyManager = CurrencyManager.Instance;
            if (shopManager == null)
                shopManager = ShopManager.Instance;

            if (inventoryManager == null) return;

            // 如果网格尚未初始化（首次打开时 Start/DelayedInit 未完成），立即初始化
            if (slotUIs == null)
            {
                InitializeGrids();
                SubscribeEvents();
            }

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                {
                    slotUIs[i].SetSlot(inventoryManager.GetSlot(i));
                }
            }

            UpdateGoldDisplay();
            ClearSelection();
        }
        #endregion

        #region Selection Logic
        private void OnInventorySlotClicked(int index)
        {
            if (inventoryManager == null) return;

            InventorySlot slot = inventoryManager.GetSlot(index);

            // 空槽位忽略
            if (slot == null || slot.IsEmpty)
            {
                ClearSelection();
                return;
            }

            // 点击已选中的 → 取消选中
            if (selectedSlotIndex == index)
            {
                ClearSelection();
                return;
            }

            // 选中新槽位
            ClearSelection();
            selectedSlotIndex = index;

            if (slotUIs[index] != null)
            {
                slotUIs[index].SetHighlight(true);
            }
        }

        private void ClearSelection()
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIs.Length && slotUIs[selectedSlotIndex] != null)
            {
                slotUIs[selectedSlotIndex].SetHighlight(false);
            }
            selectedSlotIndex = -1;
        }
        #endregion

        #region Sell Logic
        private void OnSellButtonClicked()
        {
            if (shopManager == null || inventoryManager == null) return;
            if (selectedSlotIndex < 0) return;

            InventorySlot slot = inventoryManager.GetSlot(selectedSlotIndex);
            if (slot == null || slot.IsEmpty) return;

            int sellPrice = shopManager.GetSellPrice(slot.itemRef);
            if (sellPrice <= 0)
            {
                shopManager.SellItem(selectedSlotIndex, 1); // 会触发"不可出售"提示
                return;
            }

            if (slot.count == 1)
            {
                shopManager.SellItem(selectedSlotIndex, 1);
            }
            else
            {
                int slotIdx = selectedSlotIndex;
                QuantityDialogUI.Show(
                    slot.itemRef.itemName,
                    slot.itemRef.itemIcon,
                    1,
                    slot.count,
                    (quantity) => shopManager.SellItem(slotIdx, quantity)
                );
            }

            ClearSelection();
        }
        #endregion

        #region Event Callbacks
        private void OnSlotChanged(int index)
        {
            if (slotUIs == null || index < 0 || index >= slotUIs.Length) return;
            if (slotUIs[index] == null || inventoryManager == null) return;

            slotUIs[index].SetSlot(inventoryManager.GetSlot(index));
        }

        private void OnGoldChanged(int newGold)
        {
            UpdateGoldDisplay();
        }

        private void OnItemSold(int slotIndex, int amount)
        {
            ClearSelection();
        }
        #endregion

        #region UI Updates
        private void UpdateGoldDisplay()
        {
            if (goldText != null && currencyManager != null)
            {
                goldText.text = currencyManager.CurrentGold.ToString();
            }
        }
        #endregion
    }
}
