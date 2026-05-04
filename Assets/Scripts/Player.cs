using System.Collections;
using UnityEngine;
using InventorySystem;
using Core.Constants;
using Core.Items;
using ItemSystem;
using FarmingSystem;
using AnimationSystem;

namespace Core.Player
{
    /// <summary>
    /// 玩家控制器
    /// 重构：只保留输入和移动逻辑，拾取功能委托给ItemInteractor和ItemPickupHandler
    /// </summary>
    public class Player : MonoBehaviour
    {
        #region Serialized Fields
        [Header("移动设置")]
        public float moveSpeed = 5f;
        #endregion

        #region Private Fields
        private Rigidbody rb;
        private Vector3 moveDirection;
        private Camera mainCamera;

        private ItemInteractor itemInteractor;
        private ItemPickupHandler pickupHandler;
        private InventoryManager inventoryManager;

        private PlayerAnimatorController animatorController;

        private bool isCooldown = false;
        private float cooldownTime = GameConstants.DefaultCooldownTime;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializeComponents();
        }

        private void Update()
        {
            // 工具动画播放期间锁定所有输入
            if (animatorController != null && animatorController.IsUsingTool)
            {
                moveDirection = Vector3.zero;
                return;
            }

            HandleMovementInput();
            HandleItemInteraction();
            HandleSeedPlanting();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            mainCamera = Camera.main;

            itemInteractor = GetComponent<ItemInteractor>();
            pickupHandler = GetComponent<ItemPickupHandler>();
            inventoryManager = GetComponent<InventoryManager>();

            if (itemInteractor == null)
            {
                itemInteractor = gameObject.AddComponent<ItemInteractor>();
            }
            if (pickupHandler == null)
            {
                pickupHandler = gameObject.AddComponent<ItemPickupHandler>();
            }
            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }

            animatorController = GetComponentInChildren<PlayerAnimatorController>();
        }
        #endregion

        #region Movement
        private void HandleMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (animatorController != null)
            {
                animatorController.SetMoveDirection(moveDirection);
            }
        }

        private void HandleMovement()
        {
            if (moveDirection.magnitude <= 0) return;
            MovePlayer();
        }

        private void MovePlayer()
        {
            Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        #endregion

        #region Item Interaction
        private void HandleItemInteraction()
        {
            if (!Input.GetKeyDown(KeyCode.Space) || isCooldown) return;

            if (itemInteractor.HasItemInRange())
            {
                TryPickupItem();
            }
            else if (inventoryManager != null)
            {
                TryDropItem();
            }
        }

        private void TryPickupItem()
        {
            bool success = itemInteractor.TryPickupItem();
            if (success)
            {
                StartCoroutine(Cooldown());
            }
        }

        private void TryDropItem()
        {
            Item selectedItem = inventoryManager.GetSelectedItem();
            if (selectedItem != null)
            {
                Vector3 dropPos = transform.position + transform.forward * GameConstants.DefaultDropDistance;
                DropSelectedItem(dropPos);
                StartCoroutine(Cooldown());
            }
        }

        private void DropSelectedItem(Vector3 dropPosition)
        {
            Item item = inventoryManager.RemoveItem(inventoryManager.SelectedIndex);
            if (item != null)
            {
                item.gameObject.SetActive(true);
                item.OnDropped(dropPosition);
                Debug.Log($"丢弃物品: {item.itemName}");
            }
        }

        private IEnumerator Cooldown()
        {
            if (isCooldown) yield break;
            isCooldown = true;
            yield return new WaitForSeconds(cooldownTime);
            isCooldown = false;
        }
        #endregion

        #region Seed Planting
        /// <summary>
        /// 处理种子种植
        /// </summary>
        private void HandleSeedPlanting()
        {
            // 鼠标左键点击时检测
            if (!Input.GetMouseButtonDown(0)) return;
            if (inventoryManager == null) return;

            // 检查当前选中的物品是否是种子
            Item selectedItem = inventoryManager.GetSelectedItem();
            if (selectedItem == null) return;
            if (selectedItem.itemType != ItemType.Seed) return;

            // 获取种子的CropData
            SeedItem seedItem = selectedItem as SeedItem;
            if (seedItem == null || seedItem.CropData == null) return;

            // 射线检测土堆
            SoilMound targetMound = DetectSoilMound();
            if (targetMound == null) return;

            // 检查土堆是否为空
            if (!targetMound.IsEmpty)
            {
                Debug.Log("[Player] 土堆已有作物，无法种植");
                return;
            }

            // 检查距离
            float distance = Vector3.Distance(transform.position, targetMound.transform.position);
            if (distance > 5f) // 种植距离
            {
                Debug.Log("[Player] 距离太远，无法种植");
                return;
            }

            // 尝试种植
            if (targetMound.TryPlant(seedItem.CropData, gameObject))
            {
                // 种植成功，消耗种子
                inventoryManager.RemoveItem(inventoryManager.SelectedIndex, 1);
                Debug.Log($"[Player] 种植了 {seedItem.CropData.cropName}");
            }
        }

        /// <summary>
        /// 检测鼠标位置的土堆
        /// </summary>
        private SoilMound DetectSoilMound()
        {
            if (mainCamera == null) return null;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, GameConstants.DefaultRaycastDistance))
            {
                return hit.collider.GetComponent<SoilMound>();
            }
            return null;
        }
        #endregion
    }
}
