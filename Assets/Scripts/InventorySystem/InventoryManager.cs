using System;
using UnityEngine;
using ToolSystem;
using Core.Items;
using Core.Constants;
using Core.Interfaces;
using ItemSystem;

namespace InventorySystem
{
    /// <summary>
    /// 物品栏管理器
    /// </summary>
    public class InventoryManager : MonoBehaviour, IInventory
    {
        #region Singleton
        public static InventoryManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("槽位设置")]
        [SerializeField] private int maxSlots = GameConstants.DefaultMaxSlots;

        [SerializeField] private InventorySlot[] slots;

        [Header("选中状态")]
        [SerializeField] private int selectedIndex = 0;

        [Header("装备设置")]
        [SerializeField] private Transform handPosition;
        #endregion

        #region Private Fields
        private ToolItem currentEquippedTool;
        #endregion

        #region Public Properties
        public int MaxSlots => maxSlots;
        public InventorySlot[] Slots => slots;
        public int SelectedIndex => selectedIndex;
        public int HotbarSize => GameConstants.HotbarSlots;
        public int BackpackStartIndex => HotbarSize;
        #endregion

        #region Events
        public event Action<int> OnSlotChanged;
        public event Action<int> OnSelectionChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeSingleton();
            InitializeSlots();
        }

        private void Start()
        {
            InitializeHandPosition();
            SelectSlot(0);
        }

        private void Update()
        {
            HandleInput();
        }
        #endregion

        #region Initialization
        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSlots()
        {
            slots = new InventorySlot[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                slots[i] = new InventorySlot();
            }
        }

        private void InitializeHandPosition()
        {
            if (handPosition == null)
            {
                var handler = GetComponent<ItemPickupHandler>();
                if (handler != null)
                {
                    handPosition = handler.handPosition;
                }
            }
        }
        #endregion

        #region Input Handling
        private void HandleInput()
        {
            HandleNumberKeyInput();
            HandleScrollWheelInput();
        }

        private void HandleNumberKeyInput()
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i - 1))
                {
                    SelectSlot(i - 1);
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                SelectSlot(9);
            }
        }

        private void HandleScrollWheelInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int hotbarSize = HotbarSize;
            if (scroll > 0f)
            {
                SelectSlot((selectedIndex - 1 + hotbarSize) % hotbarSize);
            }
            else if (scroll < 0f)
            {
                SelectSlot((selectedIndex + 1) % hotbarSize);
            }
        }
        #endregion

        #region IInventory Implementation
        public bool AddItem(Item item)
        {
            if (item == null) return false;

            if (TryStackItem(item)) return true;
            if (TryAddToEmptySlot(item)) return true;

            Debug.Log("物品栏已满！");
            return false;
        }

        public Item RemoveItem(int index, int amount = 1)
        {
            if (index < 0 || index >= maxSlots) return null;
            if (slots[index].IsEmpty) return null;

            // 弹出第一个物品返回给调用者
            Item returnItem = slots[index].PopItem();

            // amount > 1 时，弹出剩余的物品并销毁（用于商店出售等消耗场景）
            for (int i = 1; i < amount; i++)
            {
                Item extra = slots[index].PopItem();
                if (extra == null) break;
                Destroy(extra.gameObject);
            }

            if (returnItem is ToolItem tool && tool == currentEquippedTool)
            {
                UnequipTool();
            }

            OnSlotChanged?.Invoke(index);
            return returnItem;
        }

        public Item GetSelectedItem()
        {
            if (selectedIndex < 0 || selectedIndex >= maxSlots) return null;
            return slots[selectedIndex].itemRef;
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= maxSlots) return;

            int oldIndex = selectedIndex;
            selectedIndex = index;

            UnequipTool();

            var slot = slots[selectedIndex];
            if (!slot.IsEmpty && slot.itemRef is ToolItem tool)
            {
                EquipTool(tool);
            }

            if (oldIndex != selectedIndex)
            {
                OnSelectionChanged?.Invoke(selectedIndex);
                Debug.Log($"选中槽位 {selectedIndex + 1}");
            }
        }

        public bool IsFull()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i].IsEmpty) return false;
            }
            return true;
        }
        #endregion

        #region Slot Management
        private bool TryStackItem(Item item)
        {
            if (item.itemType == ItemType.Tool) return false;

            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i].CanStackWith(item))
                {
                    slots[i].StackItem(item);
                    item.gameObject.SetActive(false);
                    OnSlotChanged?.Invoke(i);
                    Debug.Log($"物品 {item.itemName} 堆叠到槽位 {i + 1}");
                    return true;
                }
            }
            return false;
        }

        private bool TryAddToEmptySlot(Item item)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].SetItem(item, 1);
                    item.gameObject.SetActive(false);
                    OnSlotChanged?.Invoke(i);
                    Debug.Log($"物品 {item.itemName} 放入槽位 {i + 1}");

                    if (i == selectedIndex && item.itemType == ItemType.Tool)
                    {
                        EquipTool(item as ToolItem);
                    }

                    return true;
                }
            }
            return false;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= maxSlots) return null;
            return slots[index];
        }

        /// <summary>
        /// 交换两个槽位的内容
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= maxSlots) return;
            if (indexB < 0 || indexB >= maxSlots) return;
            if (indexA == indexB) return;

            InventorySlot temp = slots[indexA];
            slots[indexA] = slots[indexB];
            slots[indexB] = temp;

            OnSlotChanged?.Invoke(indexA);
            OnSlotChanged?.Invoke(indexB);

            // 如果涉及当前选中的快捷栏槽位，重新评估工具装备
            if (indexA == selectedIndex || indexB == selectedIndex)
            {
                SelectSlot(selectedIndex);
            }
        }
        #endregion

        #region Tool Equipment
        private void EquipTool(ToolItem tool)
        {
            if (tool == null) return;

            currentEquippedTool = tool;
            tool.gameObject.SetActive(true);

            if (handPosition != null)
            {
                tool.transform.SetParent(handPosition);
                tool.transform.localPosition = tool.attachOffset;
                tool.transform.localRotation = Quaternion.identity;
            }

            Debug.Log($"装备工具: {tool.itemName}");
        }

        private void UnequipTool()
        {
            if (currentEquippedTool != null)
            {
                currentEquippedTool.gameObject.SetActive(false);
                currentEquippedTool = null;
            }
        }

        public ToolItem GetEquippedTool()
        {
            return currentEquippedTool;
        }
        #endregion
    }
}
