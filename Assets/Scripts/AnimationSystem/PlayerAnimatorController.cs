using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;
using Core.Constants;

namespace AnimationSystem
{
    /// <summary>
    /// 玩家动画参数驱动器
    /// 仅负责设置 Animator 参数，所有状态转换在 Animator Controller 中手动配置
    /// 需要 SkeletonMecanim（Spine）和 Animator 组件
    /// </summary>
    public class PlayerAnimatorController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("组件引用")]
        [Tooltip("SkeletonMecanim 组件引用（留空则自动查找）")]
        [SerializeField] private SkeletonMecanim skeletonMecanim;

        [Header("朝向设置")]
        [Tooltip("判定移动方向的最小阈值")]
        [SerializeField] private float directionThreshold = 0.1f;

        #endregion

        #region Private Fields

        private Animator animator;

        // 缓存参数哈希值以提高性能
        private int hashSpeed;
        private int hashFacingDirection;
        private int hashIsUsingTool;
        private int hashToolType;

        // 当前朝向状态
        private int currentFacingDirection = AnimationConstants.FacingFront;
        private bool isFacingRight = true;

        // 外部输入（由 Player.cs 设置）
        private Vector3 currentMoveDirection;

        // 工具动画状态
        private bool isUsingTool = false;
        private Coroutine toolAnimCoroutine;

        #endregion

        /// <summary>
        /// 是否正在播放工具动画（Player 用于锁定移动和输入）
        /// </summary>
        public bool IsUsingTool => isUsingTool;

        /// <summary>
        /// 工具动画播放完毕时触发
        /// </summary>
        public event Action OnToolAnimationFinished;

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            CacheParameterHashes();
        }

        private void Update()
        {
            UpdateFacingDirection();
            UpdateAnimatorParameters();
        }

        private void LateUpdate()
        {
            UpdateSpriteFlip();
        }

        #endregion

        #region Initialization

        private void CacheComponents()
        {
            if (skeletonMecanim == null)
            {
                skeletonMecanim = GetComponentInChildren<SkeletonMecanim>();
            }

            if (skeletonMecanim != null)
            {
                animator = skeletonMecanim.GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("[PlayerAnimatorController] 未找到 Animator！请确保 SkeletonMecanim 和 Animator 存在于当前对象或子对象上。");
            }
        }

        /// <summary>
        /// 缓存 Animator 参数字符串的哈希值
        /// </summary>
        private void CacheParameterHashes()
        {
            hashSpeed = Animator.StringToHash(AnimationConstants.ParamSpeed);
            hashFacingDirection = Animator.StringToHash(AnimationConstants.ParamFacingDirection);
            hashIsUsingTool = Animator.StringToHash(AnimationConstants.ParamIsUsingTool);
            hashToolType = Animator.StringToHash(AnimationConstants.ParamToolType);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 由 Player.cs 每帧调用，传入当前移动方向
        /// </summary>
        /// <param name="moveDirection">归一化的移动向量 (x=水平, z=垂直)</param>
        public void SetMoveDirection(Vector3 moveDirection)
        {
            currentMoveDirection = moveDirection;
        }

        /// <summary>
        /// 触发工具使用动画
        /// </summary>
        /// <param name="toolTypeValue">工具类型的整数值（对应 ToolType 枚举）</param>
        /// <param name="duration">动画持续时长（秒），播完后自动回到 idle</param>
        public void TriggerToolUse(int toolTypeValue, float duration)
        {
            if (animator == null || isUsingTool) return;

            isUsingTool = true;

            // 强制 Speed=0，让角色在工具动画期间停在原地
            animator.SetFloat(hashSpeed, 0f);
            animator.SetInteger(hashToolType, toolTypeValue);
            animator.SetBool(hashIsUsingTool, true);

            // 启动协程，等待动画播完后自动结束
            if (toolAnimCoroutine != null)
                StopCoroutine(toolAnimCoroutine);
            toolAnimCoroutine = StartCoroutine(WaitAndEndToolUse(duration));
        }

        /// <summary>
        /// 立即结束工具动画（可外部强制调用）
        /// </summary>
        public void EndToolUse()
        {
            if (animator == null) return;

            if (toolAnimCoroutine != null)
            {
                StopCoroutine(toolAnimCoroutine);
                toolAnimCoroutine = null;
            }

            animator.SetBool(hashIsUsingTool, false);
            animator.SetInteger(hashToolType, 0);
            isUsingTool = false;

            OnToolAnimationFinished?.Invoke();
        }

        private IEnumerator WaitAndEndToolUse(float duration)
        {
            yield return new WaitForSeconds(duration);
            EndToolUse();
        }

        #endregion

        #region Parameter Updates

        /// <summary>
        /// 根据移动输入判定朝向
        /// 只在移动时更新，停止时保持最后朝向
        /// 工具动画期间锁定朝向
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (isUsingTool) return;
            if (currentMoveDirection.magnitude < directionThreshold) return;

            float absX = Mathf.Abs(currentMoveDirection.x);
            float absZ = Mathf.Abs(currentMoveDirection.z);

            if (absX > absZ)
            {
                // 水平移动为主 → 侧面朝向
                currentFacingDirection = AnimationConstants.FacingSide;
                isFacingRight = currentMoveDirection.x > 0;
            }
            else
            {
                // 垂直移动为主
                if (currentMoveDirection.z > 0)
                {
                    currentFacingDirection = AnimationConstants.FacingBack;
                }
                else
                {
                    currentFacingDirection = AnimationConstants.FacingFront;
                }
            }
        }

        /// <summary>
        /// 每帧将当前状态写入 Animator 参数
        /// 工具动画期间跳过，避免覆盖状态机参数
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            if (animator == null || isUsingTool) return;

            float speed = currentMoveDirection.magnitude;
            animator.SetFloat(hashSpeed, speed);
            animator.SetInteger(hashFacingDirection, currentFacingDirection);
        }

        /// <summary>
        /// 通过 Spine 的 Skeleton.ScaleX 控制左右翻转
        /// 在 LateUpdate 中执行，确保在 Spine 动画更新之后
        /// </summary>
        private void UpdateSpriteFlip()
        {
            if (skeletonMecanim == null || skeletonMecanim.Skeleton == null) return;

            if (currentFacingDirection == AnimationConstants.FacingSide)
            {
                skeletonMecanim.Skeleton.ScaleX = isFacingRight ? 1f : -1f;
            }
            else
            {
                skeletonMecanim.Skeleton.ScaleX = 1f;
            }
        }

        #endregion
    }
}
