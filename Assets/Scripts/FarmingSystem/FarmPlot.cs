using System.Collections.Generic;
using UnityEngine;
using ToolSystem;
using Core.Constants;

namespace FarmingSystem
{
    /// <summary>
    /// 农田区域 - 可用锄头翻地生成土堆
    /// 实现IToolTarget接口接收工具作用
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FarmPlot : MonoBehaviour, IToolTarget
    {
        [Header("土堆配置")]
        [Tooltip("土堆Prefab")]
        [SerializeField] private GameObject soilMoundPrefab;

        [Tooltip("土堆之间的最小距离")]
        [SerializeField] private float minMoundDistance = 1.5f;

        [Tooltip("土堆生成的Y轴偏移")]
        [SerializeField] private float moundYOffset = 0.01f;

        [Header("区域限制")]
        [Tooltip("允许的最大土堆数量 (0=无限制)")]
        [SerializeField] private int maxMounds = 0;

        [Header("调试")]
        [SerializeField] private bool showDebugGizmos = true;

        // 已生成的土堆列表
        private List<SoilMound> mounds = new List<SoilMound>();

        // 缓存的碰撞体
        private Collider plotCollider;

        #region Unity Lifecycle

        private void Awake()
        {
            plotCollider = GetComponent<Collider>();
        }

        #endregion

        #region IToolTarget Implementation

        public bool CanInteract(ToolItem tool)
        {
            // 只接受锄头
            if (tool == null || tool.ToolType != ToolType.Hoe)
            {
                return false;
            }

            // 检查是否达到最大土堆数
            if (maxMounds > 0 && mounds.Count >= maxMounds)
            {
                Debug.Log("[FarmPlot] 已达到最大土堆数量");
                return false;
            }

            return true;
        }

        public void ReceiveToolAction(ToolItem tool, GameObject user)
        {
            if (tool.ToolType == ToolType.Hoe)
            {
                TryCreateMoundAtMousePosition();
            }
        }

        public ToolType[] GetAcceptedToolTypes()
        {
            return new ToolType[] { ToolType.Hoe };
        }

        #endregion

        #region Mound Creation

        /// <summary>
        /// 在鼠标位置尝试生成土堆
        /// </summary>
        private void TryCreateMoundAtMousePosition()
        {
            // 获取鼠标点击的世界坐标
            Vector3? hitPoint = GetMouseWorldPosition();
            if (!hitPoint.HasValue)
            {
                Debug.Log("[FarmPlot] 无法获取有效的鼠标位置");
                return;
            }

            Vector3 spawnPosition = hitPoint.Value;
            spawnPosition.y += moundYOffset;

            // 检查与现有土堆的距离
            if (!IsValidMoundPosition(spawnPosition))
            {
                Debug.Log("[FarmPlot] 距离其他土堆太近，无法生成");
                return;
            }

            // 生成土堆
            CreateMound(spawnPosition);
        }

        /// <summary>
        /// 获取鼠标在农田上的世界坐标
        /// </summary>
        private Vector3? GetMouseWorldPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[FarmPlot] 未找到主摄像机");
                return null;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 检测射线与农田的交点
            if (Physics.Raycast(ray, out RaycastHit hit, GameConstants.DefaultRaycastDistance))
            {
                // 确保点击的是这个农田
                if (hit.collider == plotCollider)
                {
                    return hit.point;
                }

                // 如果点击的是土堆，仍然获取位置（但会被距离检查阻止）
                SoilMound mound = hit.collider.GetComponent<SoilMound>();
                if (mound != null && mounds.Contains(mound))
                {
                    return hit.point;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查位置是否有效（与其他土堆保持最小距离）
        /// </summary>
        private bool IsValidMoundPosition(Vector3 position)
        {
            // 清理已销毁的土堆引用
            CleanupDestroyedMounds();

            foreach (SoilMound mound in mounds)
            {
                if (mound == null) continue;

                float distance = Vector3.Distance(position, mound.transform.position);
                if (distance < minMoundDistance)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 创建土堆
        /// </summary>
        private void CreateMound(Vector3 position)
        {
            if (soilMoundPrefab == null)
            {
                Debug.LogError("[FarmPlot] 未设置土堆Prefab!");
                return;
            }

            // 实例化土堆
            GameObject moundObj = Instantiate(soilMoundPrefab, position, Quaternion.identity, transform);

            // 获取并初始化SoilMound组件
            SoilMound mound = moundObj.GetComponent<SoilMound>();
            if (mound != null)
            {
                mound.Initialize(this);
                mounds.Add(mound);
                Debug.Log($"[FarmPlot] 在 {position} 生成土堆，当前土堆数量: {mounds.Count}");
            }
            else
            {
                Debug.LogError("[FarmPlot] 土堆Prefab缺少SoilMound组件!");
                Destroy(moundObj);
            }
        }

        /// <summary>
        /// 清理已销毁的土堆引用
        /// </summary>
        private void CleanupDestroyedMounds()
        {
            mounds.RemoveAll(m => m == null);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 移除土堆（由SoilMound调用）
        /// </summary>
        public void RemoveMound(SoilMound mound)
        {
            if (mounds.Contains(mound))
            {
                mounds.Remove(mound);
                Debug.Log($"[FarmPlot] 土堆已移除，剩余土堆数量: {mounds.Count}");
            }
        }

        /// <summary>
        /// 获取当前土堆数量
        /// </summary>
        public int GetMoundCount()
        {
            CleanupDestroyedMounds();
            return mounds.Count;
        }

        /// <summary>
        /// 获取所有土堆
        /// </summary>
        public List<SoilMound> GetAllMounds()
        {
            CleanupDestroyedMounds();
            return new List<SoilMound>(mounds);
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // 绘制农田边界
            Gizmos.color = new Color(0.5f, 0.3f, 0.1f, 0.2f);
            if (plotCollider != null)
            {
                Gizmos.DrawCube(plotCollider.bounds.center, plotCollider.bounds.size);
            }

            // 绘制土堆最小距离范围
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            foreach (var mound in mounds)
            {
                if (mound != null)
                {
                    Gizmos.DrawWireSphere(mound.transform.position, minMoundDistance / 2);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 选中时绘制更明显的边界
            Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.5f);
            if (plotCollider != null)
            {
                Gizmos.DrawWireCube(plotCollider.bounds.center, plotCollider.bounds.size);
            }
        }

        #endregion
    }
}
