using UnityEngine;
using Core.Items;
using Core.Constants;
using InventorySystem;

namespace FarmingSystem
{
    /// <summary>
    /// 种子物品 - 可种植到土堆中
    /// 继承自Item基类，设置为Seed类型
    /// </summary>
    public class SeedItem : Item
    {
        [Header("种子配置")]
        [Tooltip("关联的作物数据")]
        [SerializeField] private CropData cropData;

        [Header("种植设置")]
        [Tooltip("种植检测距离")]
        [SerializeField] private float plantingRange = 3f;

        #region Public Properties

        public CropData CropData => cropData;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // 确保物品类型为Seed
            itemType = ItemType.Seed;
        }

        protected virtual void Update()
        {
            // 只有在物品栏中被选中时才检测输入
            if (!IsSelectedInInventory())
            {
                return;
            }

            // 鼠标左键尝试种植
            if (Input.GetMouseButtonDown(0))
            {
                TryPlant();
            }
        }

        #endregion

        #region Planting Logic

        /// <summary>
        /// 检查此种子是否在物品栏中被选中
        /// </summary>
        private bool IsSelectedInInventory()
        {
            if (InventoryManager.Instance == null) return false;

            Item selectedItem = InventoryManager.Instance.GetSelectedItem();
            if (selectedItem == null) return false;

            // 检查选中的物品是否是这个种子（或相同类型的种子）
            if (selectedItem is SeedItem seed)
            {
                return seed.cropData == this.cropData;
            }

            return false;
        }

        /// <summary>
        /// 尝试种植
        /// </summary>
        private void TryPlant()
        {
            // 检测土堆
            SoilMound targetMound = DetectSoilMound();
            if (targetMound == null)
            {
                return; // 没有检测到土堆，不输出日志（避免频繁输出）
            }

            // 检查土堆状态
            if (!targetMound.IsEmpty)
            {
                Debug.Log("[SeedItem] 土堆已有作物，无法种植");
                return;
            }

            // 获取种植者（玩家）
            GameObject planter = GetPlanter();
            if (planter == null)
            {
                Debug.LogWarning("[SeedItem] 无法获取种植者");
                return;
            }

            // 检查距离
            float distance = Vector3.Distance(planter.transform.position, targetMound.transform.position);
            if (distance > plantingRange)
            {
                Debug.Log("[SeedItem] 距离太远，无法种植");
                return;
            }

            // 尝试种植
            if (targetMound.TryPlant(cropData, planter))
            {
                // 种植成功，消耗种子
                ConsumeSeed();
            }
        }

        /// <summary>
        /// 检测鼠标位置的土堆
        /// </summary>
        private SoilMound DetectSoilMound()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return null;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, GameConstants.DefaultRaycastDistance))
            {
                return hit.collider.GetComponent<SoilMound>();
            }

            return null;
        }

        /// <summary>
        /// 获取种植者（玩家）
        /// </summary>
        private GameObject GetPlanter()
        {
            // 尝试从物品栏获取玩家
            if (InventoryManager.Instance != null)
            {
                return InventoryManager.Instance.gameObject;
            }

            // 备选：查找Player标签
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player;
        }

        /// <summary>
        /// 消耗种子
        /// </summary>
        private void ConsumeSeed()
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory != null)
            {
                // 从物品栏移除一个种子
                inventory.RemoveItem(inventory.SelectedIndex, 1);
                Debug.Log($"[SeedItem] 消耗了 1 个 {itemName}");
            }
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            // 确保物品类型正确
            if (itemType != ItemType.Seed)
            {
                itemType = ItemType.Seed;
            }

            // 如果有关联的作物数据，自动设置名称
            if (cropData != null)
            {
                if (string.IsNullOrEmpty(itemName) || itemName == "Test Item")
                {
                    itemName = $"{cropData.cropName} Seed";
                }

                if (string.IsNullOrEmpty(itemID) || itemID == "item_001")
                {
                    itemID = $"seed_{cropData.cropID}";
                }
            }
        }

        #endregion
    }
}
