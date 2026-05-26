using System.Collections.Generic;
using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// 场景级 Billboard：让场景中所有 SpriteRenderer 的 Transform 始终朝向主摄像机。
    /// 挂载到场景中任意一个常驻 GameObject 上即可（例如空对象 "_Billboards"）。
    /// </summary>
    public class SpriteBillboard : MonoBehaviour
    {
        [Header("摄像机")]
        [Tooltip("留空时使用 Camera.main")]
        public Camera targetCamera;

        [Header("刷新")]
        [Tooltip("自动周期性扫描场景，捕获运行时新生成的 SpriteRenderer。<=0 表示关闭自动刷新")]
        public float autoRescanInterval = 2f;

        private readonly List<Transform> targets = new List<Transform>();
        private float rescanTimer;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            Rescan();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            if (autoRescanInterval > 0f)
            {
                rescanTimer += Time.deltaTime;
                if (rescanTimer >= autoRescanInterval)
                {
                    rescanTimer = 0f;
                    Rescan();
                }
            }

            Quaternion camRot = targetCamera.transform.rotation;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                Transform t = targets[i];
                if (t == null)
                {
                    targets.RemoveAt(i);
                    continue;
                }
                t.rotation = camRot;
            }
        }

        /// <summary>
        /// 重新扫描场景，收集所有 SpriteRenderer 所在的 Transform。
        /// 运行时动态生成新精灵后可手动调用一次以立即纳入管理。
        /// </summary>
        public void Rescan()
        {
            targets.Clear();
            SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                targets.Add(renderers[i].transform);
            }
        }
    }
}
