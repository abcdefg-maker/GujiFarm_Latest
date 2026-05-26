using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// 摄像机跟随：在水平面（XZ）跟随目标，Y 高度保持固定。
    /// 直接挂载到主摄像机上使用，类似饥荒的俯视跟随。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随目标")]
        [Tooltip("留空时自动查找 Tag 为 Player 的对象")]
        public Transform target;

        [Header("高度设置")]
        [Tooltip("是否在运行时锁定当前 Y 作为高度。关闭则使用 fixedHeight")]
        public bool useCurrentYAsHeight = true;

        [Tooltip("当 useCurrentYAsHeight 关闭时使用的固定高度")]
        public float fixedHeight = 10f;

        [Header("跟随参数")]
        [Tooltip("是否启用平滑跟随")]
        public bool smoothFollow = true;

        [Tooltip("平滑跟随到达目标所需的近似时间（秒），值越小越跟手")]
        public float smoothTime = 0.15f;

        [Tooltip("相对目标的 XZ 偏移（X 水平，Y 字段映射到 Z 方向）")]
        public Vector2 offsetXZ = Vector2.zero;

        private Vector3 currentVelocity;
        private float lockedY;

        private void Awake()
        {
            lockedY = useCurrentYAsHeight ? transform.position.y : fixedHeight;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(
                target.position.x + offsetXZ.x,
                lockedY,
                target.position.z + offsetXZ.y
            );

            if (smoothFollow)
            {
                Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, smoothTime);
                next.y = lockedY;
                transform.position = next;
            }
            else
            {
                transform.position = desired;
            }
        }

        /// <summary>
        /// 运行时调整高度（例如缩放视角）。
        /// </summary>
        public void SetHeight(float y)
        {
            lockedY = y;
        }
    }
}
