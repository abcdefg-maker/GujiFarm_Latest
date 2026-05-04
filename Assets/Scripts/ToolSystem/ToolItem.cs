using UnityEngine;
using ToolSystem.Actions;
using Core.Constants;
using Core.Items;
using Core.Interfaces;
using AnimationSystem;

namespace ToolSystem
{
    /// <summary>
    /// 工具物品基类 - 继承自Item
    /// </summary>
    public class ToolItem : Item, IEquippable
    {
        [Header("工具设置")]
        [Tooltip("工具类型")]
        [SerializeField] protected ToolType toolType = ToolType.None;
        public ToolType ToolType => toolType;

        [Header("工具行为")]
        [Tooltip("该工具可执行的行为（ScriptableObject配置）")]
        [SerializeField] protected ScriptableToolAction toolAction;
        public ScriptableToolAction ToolAction => toolAction;

        private bool isEquipped = false;
        public bool IsEquipped => isEquipped;

        protected virtual void Awake()
        {
            itemType = ItemType.Tool;
        }

        protected virtual void Update()
        {
            // 只有被持有时才检测输入
            if (!IsPickedUp || CurrentHolder == null)
            {
                return;
            }

            // 鼠标左键使用工具（动画播放中不可重复使用）
            if (Input.GetMouseButtonDown(0))
            {
                var animController = CurrentHolder.GetComponentInChildren<PlayerAnimatorController>();
                if (animController != null && animController.IsUsingTool) return;

                IToolTarget target = DetectTarget();
                TryUse(CurrentHolder, target);
            }
        }

        /// <summary>
        /// 检测工具作用目标（子类可重写）
        /// </summary>
        protected virtual IToolTarget DetectTarget()
        {
            // 安全检查 Camera.main
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[ToolItem] Camera.main is null!");
                return null;
            }

            // 从鼠标位置发射射线检测目标
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, GameConstants.DefaultRaycastDistance))
            {
                Debug.Log("Interacted with " + hit.collider.name);
                return hit.collider.GetComponent<IToolTarget>();
            }
            return null;
        }

        /// <summary>
        /// 尝试使用工具
        /// </summary>
        public virtual bool TryUse(GameObject user, IToolTarget target = null)
        {
            if (target != null && !target.CanInteract(this))
            {
                Debug.Log($"{itemName} 无法作用于该目标");
                return false;
            }

            OnUse(user, target);
            return true;
        }

        /// <summary>
        /// 执行工具使用（子类可重写）
        /// </summary>
        protected virtual void OnUse(GameObject user, IToolTarget target)
        {
            // 触发工具动画
            var animController = user.GetComponentInChildren<PlayerAnimatorController>();
            if (animController != null && !animController.IsUsingTool)
            {
                float duration = GetAnimationDuration();
                animController.TriggerToolUse((int)toolType, duration);
            }

            if (toolAction != null)
            {
                toolAction.Execute(this, user, target);
            }

            Debug.Log($"{user.name} 使用了 {itemName}");
        }

        /// <summary>
        /// 根据工具类型返回对应动画时长
        /// </summary>
        protected virtual float GetAnimationDuration()
        {
            return toolType switch
            {
                ToolType.WateringCan => AnimationConstants.WateringDuration,
                ToolType.Hoe => AnimationConstants.HoeingDuration,
                ToolType.Sickle => AnimationConstants.SickleDuration,
                _ => 1f // 其他工具默认1秒
            };
        }

        #region IEquippable Implementation
        /// <summary>
        /// 装备工具到指定手部位置
        /// </summary>
        public virtual void Equip(Transform hand)
        {
            if (hand == null) return;

            isEquipped = true;
            gameObject.SetActive(true);
            transform.SetParent(hand);
            transform.localPosition = attachOffset;
            transform.localRotation = Quaternion.identity;

            Debug.Log($"装备工具: {itemName}");
        }

        /// <summary>
        /// 卸下工具
        /// </summary>
        public virtual void Unequip()
        {
            isEquipped = false;
            gameObject.SetActive(false);

            Debug.Log($"卸下工具: {itemName}");
        }
        #endregion
    }
}
