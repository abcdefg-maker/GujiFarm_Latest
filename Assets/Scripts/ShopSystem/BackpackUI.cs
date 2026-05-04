using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Core.Constants;
using InventorySystem;

namespace ShopSystem
{
    /// <summary>
    /// 独立背包界面 — 默认隐藏，按B键开关
    /// 显示40个背包槽位（槽位10-49），只读查看
    ///
    /// 重要：此脚本必须挂在一个始终激活的父物体上（如BackpackRoot），
    /// backpackPanel引用指向子面板。不要挂在backpackPanel自身上，
    /// 否则SetActive(false)后Update不再运行，无法检测按键。
    /// </summary>
    public class BackpackUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("面板引用")]
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private Transform gridParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button closeButton;
        #endregion

        #region Private Fields
        private InventoryManager inventoryManager;
        private ShopInventorySlotUI[] slotUIs;
        private bool initialized;
        private bool isVisible;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (backpackPanel != null)
                backpackPanel.SetActive(false);

            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            inventoryManager = InventoryManager.Instance;

            if (inventoryManager == null)
            {
                Debug.LogError("[BackpackUI] 找不到 InventoryManager!");
                yield break;
            }

            inventoryManager.OnSlotChanged += OnSlotChanged;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            initialized = true;
            Debug.Log("[BackpackUI] 初始化完成");
        }

        private void Update()
        {
            if (!initialized) return;

            // B键切换背包
            if (Input.GetKeyDown(KeyCode.B))
            {
                // 商店打开时不响应B键
                if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
                    return;

                Toggle();
            }
        }

        private void OnDestroy()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnSlotChanged -= OnSlotChanged;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }
        #endregion

        #region Public Methods
        public void Toggle()
        {
            if (isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (!initialized) return;

            // 延迟创建网格（首次打开时）
            if (slotUIs == null)
            {
                InitializeGrid();
            }

            RefreshAllSlots();

            if (backpackPanel != null)
                backpackPanel.SetActive(true);

            isVisible = true;
        }

        public void Hide()
        {
            if (backpackPanel != null)
                backpackPanel.SetActive(false);

            isVisible = false;
        }
        #endregion

        #region Private Methods
        private void InitializeGrid()
        {
            int backpackSlots = GameConstants.BackpackSlots;
            int backpackStart = inventoryManager.BackpackStartIndex;

            slotUIs = new ShopInventorySlotUI[backpackSlots];

            if (gridParent == null || slotPrefab == null) return;

            for (int i = 0; i < backpackSlots; i++)
            {
                int slotIndex = backpackStart + i;
                GameObject slotObj = Instantiate(slotPrefab, gridParent);
                ShopInventorySlotUI slotUI = slotObj.GetComponent<ShopInventorySlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(slotIndex);
                    slotUIs[i] = slotUI;

                    // 添加拖拽行为，支持与快捷栏交换物品
                    DraggableSlotUI draggable = slotObj.GetComponent<DraggableSlotUI>();
                    if (draggable == null)
                    {
                        draggable = slotObj.AddComponent<DraggableSlotUI>();
                    }
                    draggable.Initialize(slotIndex);
                }
            }
        }

        private void RefreshAllSlots()
        {
            if (slotUIs == null || inventoryManager == null) return;

            int backpackStart = inventoryManager.BackpackStartIndex;

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                {
                    int slotIndex = backpackStart + i;
                    slotUIs[i].SetSlot(inventoryManager.GetSlot(slotIndex));
                }
            }
        }

        private void OnSlotChanged(int index)
        {
            if (!isVisible || slotUIs == null || inventoryManager == null) return;

            int backpackStart = inventoryManager.BackpackStartIndex;
            int localIndex = index - backpackStart;

            if (localIndex >= 0 && localIndex < slotUIs.Length && slotUIs[localIndex] != null)
            {
                slotUIs[localIndex].SetSlot(inventoryManager.GetSlot(index));
            }
        }
        #endregion
    }
}
