using UnityEngine;
using ShopSystem;

namespace InventorySystem
{
    /// <summary>
    /// 物品栏UI控制器
    /// 使用ShopInventorySlotUI统一显示（支持数量文字和拖拽交换）
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private GameObject inventoryPanel;

        [Header("槽位UI")]
        [SerializeField] private ShopInventorySlotUI[] slotUIs;

        private void Start()
        {
            // 延迟初始化，确保 InventoryManager 已经初始化
            StartCoroutine(DelayedInit());
        }

        private System.Collections.IEnumerator DelayedInit()
        {
            // 等待一帧，确保其他脚本的 Awake/Start 已执行
            yield return null;

            // 自动查找InventoryManager
            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }

            if (inventoryManager == null)
            {
                Debug.LogError("InventoryUI: 找不到 InventoryManager!");
                yield break;
            }

            Debug.Log("InventoryUI: 找到 InventoryManager，开始初始化");

            // 初始化槽位UI
            InitializeSlotUIs();

            // 订阅事件
            inventoryManager.OnSlotChanged += OnSlotChanged;
            inventoryManager.OnSelectionChanged += OnSelectionChanged;

            // 初始刷新
            RefreshUI();

            Debug.Log($"InventoryUI: 初始化完成，槽位数量={slotUIs.Length}");
        }

        private void OnDestroy()
        {
            if (inventoryManager != null)
            {
                inventoryManager.OnSlotChanged -= OnSlotChanged;
                inventoryManager.OnSelectionChanged -= OnSelectionChanged;
            }
        }

        /// <summary>
        /// 初始化槽位UI
        /// </summary>
        private void InitializeSlotUIs()
        {
            if (slotUIs == null || slotUIs.Length == 0)
            {
                // 尝试从子对象获取
                slotUIs = GetComponentsInChildren<ShopInventorySlotUI>();
            }

            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                {
                    slotUIs[i].Initialize(i);

                    // 动态添加拖拽行为
                    DraggableSlotUI draggable = slotUIs[i].GetComponent<DraggableSlotUI>();
                    if (draggable == null)
                    {
                        draggable = slotUIs[i].gameObject.AddComponent<DraggableSlotUI>();
                    }
                    draggable.Initialize(i);
                }
            }
        }

        /// <summary>
        /// 刷新所有槽位UI
        /// </summary>
        public void RefreshUI()
        {
            if (inventoryManager == null) return;

            for (int i = 0; i < slotUIs.Length && i < inventoryManager.MaxSlots; i++)
            {
                UpdateSlot(i);
            }

            // 更新选中状态
            UpdateSelectionHighlight(inventoryManager.SelectedIndex);
        }

        /// <summary>
        /// 更新单个槽位
        /// </summary>
        public void UpdateSlot(int index)
        {
            if (index < 0 || index >= slotUIs.Length) return;
            if (slotUIs[index] == null) return;

            var slot = inventoryManager.GetSlot(index);
            slotUIs[index].SetSlot(slot);
        }

        /// <summary>
        /// 更新选中高亮
        /// </summary>
        private void UpdateSelectionHighlight(int selectedIndex)
        {
            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                {
                    slotUIs[i].SetHighlight(i == selectedIndex);
                }
            }
        }

        /// <summary>
        /// 槽位内容变化回调
        /// </summary>
        private void OnSlotChanged(int index)
        {
            UpdateSlot(index);
        }

        /// <summary>
        /// 选中槽位变化回调
        /// </summary>
        private void OnSelectionChanged(int index)
        {
            UpdateSelectionHighlight(index);
        }

        /// <summary>
        /// 显示物品栏
        /// </summary>
        public void Show()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏物品栏
        /// </summary>
        public void Hide()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }
    }
}
