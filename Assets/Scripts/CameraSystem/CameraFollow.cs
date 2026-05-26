using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// 摄像机跟随 + 绕目标 Y 轴旋转。
    /// 在水平面跟随目标，按键可左右切换视角（饥荒风格）。
    /// 直接挂载到主摄像机上使用。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随目标")]
        [Tooltip("留空时自动查找 Tag 为 Player 的对象")]
        public Transform target;

        [Header("跟随参数")]
        [Tooltip("是否启用平滑跟随")]
        public bool smoothFollow = true;

        [Tooltip("平滑跟随到达目标所需的近似时间（秒），值越小越跟手")]
        public float smoothTime = 0.15f;

        [Header("视角旋转")]
        [Tooltip("向左旋转按键")]
        public KeyCode rotateLeftKey = KeyCode.L;

        [Tooltip("向右旋转按键")]
        public KeyCode rotateRightKey = KeyCode.R;

        [Tooltip("旋转模式：阶梯式按一次转固定角度，连续式按住持续旋转")]
        public RotateMode rotateMode = RotateMode.Stepped;

        [Tooltip("阶梯模式：每次按键旋转的角度")]
        public float stepAngle = 45f;

        [Tooltip("旋转过渡速度（度/秒），同时用作连续模式的旋转速度")]
        public float rotationSpeed = 360f;

        [Header("玩家联动")]
        [Tooltip("是否让目标（玩家）始终正对相机")]
        public bool rotateTargetToFaceCamera = true;

        public enum RotateMode { Stepped, Continuous }

        private Vector3 initialOffset;
        private Quaternion initialRotation;
        private float currentYaw;
        private float targetYaw;
        private Vector3 positionVelocity;

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

            if (target != null)
            {
                initialOffset = transform.position - target.position;
            }
            else
            {
                initialOffset = transform.position;
            }
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            HandleRotationInput();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (rotateMode == RotateMode.Stepped)
            {
                currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);
            }

            Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);
            Vector3 desired = target.position + yawRot * initialOffset;

            if (smoothFollow)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref positionVelocity, smoothTime);
            }
            else
            {
                transform.position = desired;
            }

            transform.rotation = yawRot * initialRotation;

            if (rotateTargetToFaceCamera)
            {
                Vector3 toCamera = transform.position - target.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    target.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                }
            }
        }

        private void HandleRotationInput()
        {
            if (rotateMode == RotateMode.Stepped)
            {
                if (Input.GetKeyDown(rotateLeftKey))
                {
                    targetYaw -= stepAngle;
                }
                if (Input.GetKeyDown(rotateRightKey))
                {
                    targetYaw += stepAngle;
                }
            }
            else
            {
                if (Input.GetKey(rotateLeftKey))
                {
                    currentYaw -= rotationSpeed * Time.deltaTime;
                    targetYaw = currentYaw;
                }
                if (Input.GetKey(rotateRightKey))
                {
                    currentYaw += rotationSpeed * Time.deltaTime;
                    targetYaw = currentYaw;
                }
            }
        }

        /// <summary>外部重置视角朝向。</summary>
        public void ResetView()
        {
            targetYaw = 0f;
        }
    }
}
