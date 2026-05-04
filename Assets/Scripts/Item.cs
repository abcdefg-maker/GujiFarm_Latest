using UnityEngine;
using Core.Constants;
using Core.Interfaces;

namespace Core.Items
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Material,      // 材料
        Tool,          // 工具
        Consumable,    // 消耗品
        Seed,          // 种子
        Crop           // 农作物
    }

    /// <summary>
    /// 物品基类 - 场景中的物品表现
    /// </summary>
    public class Item : MonoBehaviour, IItem
    {
        #region Serialized Fields
        [Header("物品信息")]
        [Tooltip("物品唯一标识")]
        public string itemID = "item_001";

        [Tooltip("物品显示名称")]
        public string itemName = "Test Item";

        [Tooltip("物品类型")]
        public ItemType itemType = ItemType.Material;

        [Header("物品栏显示")]
        [Tooltip("物品图标（用于UI显示）")]
        public Sprite itemIcon;

        [Header("经济属性")]
        [Tooltip("出售价格，0表示不可出售")]
        public int sellPrice = 0;

        [Header("拾取设置")]
        [Tooltip("是否可以被拾取")]
        public bool canBePickedUp = true;

        [Tooltip("拾取时的附着点偏移")]
        public Vector3 attachOffset = Vector3.zero;
        #endregion

        #region Private Fields
        [Header("状态")]
        private bool isPickedUp = false;
        private GameObject currentHolder;
        #endregion

        #region Public Properties
        public bool IsPickedUp => isPickedUp;
        public GameObject CurrentHolder => currentHolder;
        #endregion

        #region IItem Interface Properties
        string IItem.ItemID => itemID;
        string IItem.ItemName => itemName;
        ItemType IItem.ItemType => itemType;
        Sprite IItem.ItemIcon => itemIcon;
        bool IItem.CanBePickedUp => canBePickedUp;
        #endregion

        #region Pickup/Drop Methods
        /// <summary>
        /// 被拾取时调用（虚方法，子类可重写）
        /// </summary>
        public virtual void OnPickedUp(GameObject picker)
        {
            isPickedUp = true;
            currentHolder = picker;

            Debug.Log($"{itemName} was picked up by {picker.name}");
        }

        /// <summary>
        /// 被放置时调用（虚方法，子类可重写）
        /// </summary>
        public virtual void OnDropped(Vector3 dropPosition)
        {
            isPickedUp = false;
            currentHolder = null;

            DetachFromParent();

            // 先将物品移动到丢弃位置
            transform.position = dropPosition;

            // 然后调整到地面高度
            PositionOnGround();
            RestorePhysicsState();

            Debug.Log($"{itemName} was dropped at {transform.position}");
        }
        #endregion

        #region Drop Helper Methods
        private void DetachFromParent()
        {
            transform.SetParent(null);
        }

        private void PositionOnGround()
        {
            Vector3 currentPos = transform.position;
            Ray ray = new Ray(
                new Vector3(currentPos.x, currentPos.y + GameConstants.RaycastHeightOffset, currentPos.z),
                Vector3.down
            );

            if (Physics.Raycast(ray, out RaycastHit hit, GameConstants.DefaultRaycastDistance))
            {
                transform.position = new Vector3(
                    currentPos.x,
                    hit.point.y + GameConstants.GroundOffset,
                    currentPos.z
                );
            }
        }

        private void RestorePhysicsState()
        {
            if (TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = true;
            }

            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        #endregion
    }
}
