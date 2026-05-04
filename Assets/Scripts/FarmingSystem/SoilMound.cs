using UnityEngine;
using ToolSystem;
using TimeSystem;
using SanitySystem;

namespace FarmingSystem
{
    /// <summary>
    /// 土堆 - 作物种植的载体
    /// 实现IToolTarget接口，根据状态接受不同的工具交互
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SoilMound : MonoBehaviour, IToolTarget
    {
        [Header("土堆状态")]
        [SerializeField] private SoilState currentState = SoilState.Empty;

        [Header("视觉设置")]
        [Tooltip("土堆的SpriteRenderer")]
        [SerializeField] private SpriteRenderer soilRenderer;

        [Tooltip("土堆精灵（土壤的图片）")]
        [SerializeField] private Sprite soilSprite;

        [Tooltip("浇水后的土堆精灵（可选，显示湿润效果）")]
        [SerializeField] private Sprite wateredSoilSprite;

        [Header("作物位置")]
        [Tooltip("作物精灵的Y轴偏移")]
        [SerializeField] private float cropYOffset = 0.1f;

        // 引用
        private FarmPlot parentPlot;
        private CropBase currentCrop;

        #region Public Properties

        public SoilState CurrentState => currentState;
        public CropBase CurrentCrop => currentCrop;
        public bool IsEmpty => currentState == SoilState.Empty;
        public bool HasCrop => currentCrop != null;

        #endregion

        #region Enums

        public enum SoilState
        {
            Empty,      // 空，可种植
            Planted,    // 已种植，作物生长中
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (soilRenderer == null)
            {
                soilRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            UpdateVisual();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化土堆（由FarmPlot调用）
        /// </summary>
        public void Initialize(FarmPlot plot)
        {
            parentPlot = plot;
            currentState = SoilState.Empty;
            UpdateVisual();
        }

        #endregion

        #region IToolTarget Implementation

        public bool CanInteract(ToolItem tool)
        {
            if (tool == null) return false;

            switch (tool.ToolType)
            {
                case ToolType.WateringCan:
                    // 只有有作物且作物可以浇水时才能浇水
                    return currentCrop != null && currentCrop.CanBeWatered && !currentCrop.IsHarvestable;

                case ToolType.Sickle:
                    // 只有作物成熟时才能收割
                    return currentCrop != null && currentCrop.IsHarvestable;

                default:
                    return false;
            }
        }

        public void ReceiveToolAction(ToolItem tool, GameObject user)
        {
            switch (tool.ToolType)
            {
                case ToolType.WateringCan:
                    WaterCrop();
                    break;

                case ToolType.Sickle:
                    HarvestCrop(user);
                    break;
            }
        }

        public ToolType[] GetAcceptedToolTypes()
        {
            return new ToolType[] { ToolType.WateringCan, ToolType.Sickle };
        }

        #endregion

        #region Planting

        /// <summary>
        /// 尝试种植作物（由SeedItem调用）
        /// </summary>
        /// <param name="cropData">作物数据</param>
        /// <param name="planter">种植者</param>
        /// <returns>是否种植成功</returns>
        public bool TryPlant(CropData cropData, GameObject planter)
        {
            if (currentState != SoilState.Empty)
            {
                Debug.Log("[SoilMound] 土堆已有作物，无法种植");
                return false;
            }

            if (cropData == null)
            {
                Debug.LogError("[SoilMound] CropData为空!");
                return false;
            }

            // 检查季节限制
            if (TimeManager.Instance != null)
            {
                Season currentSeason = TimeManager.Instance.CurrentSeason;
                if (!cropData.CanPlantInSeason(currentSeason))
                {
                    Debug.Log($"[SoilMound] {cropData.cropName} 不能在 {currentSeason} 种植");
                    return false;
                }
            }

            // 变异检查
            CropData actualCropData = cropData;
            if (SanityManager.Instance != null)
            {
                actualCropData = SanityManager.Instance.EvaluateMutation(cropData);
            }

            // 创建作物
            CreateCrop(actualCropData);

            currentState = SoilState.Planted;
            UpdateVisual();

            Debug.Log($"[SoilMound] {planter.name} 种植了 {actualCropData.cropName}");
            return true;
        }

        /// <summary>
        /// 创建作物实例
        /// </summary>
        private void CreateCrop(CropData cropData)
        {
            // 创建作物GameObject作为土堆子对象
            GameObject cropObj = new GameObject($"Crop_{cropData.cropID}");
            cropObj.transform.SetParent(transform);
            cropObj.transform.localPosition = new Vector3(0, cropYOffset, 0);

            // 添加SpriteRenderer
            SpriteRenderer cropRenderer = cropObj.AddComponent<SpriteRenderer>();
            cropRenderer.sortingOrder = soilRenderer != null ? soilRenderer.sortingOrder + 1 : 1;

            // 添加CropBase组件并初始化
            currentCrop = cropObj.AddComponent<CropBase>();
            currentCrop.Initialize(cropData, this, cropRenderer);
        }

        #endregion

        #region Watering

        /// <summary>
        /// 浇水
        /// </summary>
        private void WaterCrop()
        {
            if (currentCrop == null)
            {
                Debug.Log("[SoilMound] 没有作物可以浇水");
                return;
            }

            bool success = currentCrop.Water();
            if (success)
            {
                // 显示浇水视觉效果（如果有）
                if (wateredSoilSprite != null && soilRenderer != null)
                {
                    soilRenderer.sprite = wateredSoilSprite;
                    // 一段时间后恢复
                    Invoke(nameof(ResetSoilSprite), 0.5f);
                }
                Debug.Log("[SoilMound] 浇水成功");
            }
        }

        /// <summary>
        /// 重置土堆精灵（浇水效果结束后）
        /// </summary>
        private void ResetSoilSprite()
        {
            if (soilRenderer != null)
            {
                soilRenderer.sprite = soilSprite;
            }
        }

        #endregion

        #region Harvesting

        /// <summary>
        /// 收获作物
        /// </summary>
        private void HarvestCrop(GameObject harvester)
        {
            if (currentCrop == null || !currentCrop.IsHarvestable)
            {
                Debug.Log("[SoilMound] 没有可收获的作物");
                return;
            }

            // 执行收获
            currentCrop.Harvest(harvester);

            // 清理土堆
            ClearMound();
        }

        /// <summary>
        /// 清理土堆（收获后销毁）
        /// </summary>
        public void ClearMound()
        {
            // 销毁作物
            if (currentCrop != null)
            {
                Destroy(currentCrop.gameObject);
                currentCrop = null;
            }

            // 通知父农田移除此土堆
            if (parentPlot != null)
            {
                parentPlot.RemoveMound(this);
            }

            Debug.Log("[SoilMound] 土堆已清理");

            // 销毁土堆自身
            Destroy(gameObject);
        }

        #endregion

        #region Visual

        /// <summary>
        /// 更新土堆视觉
        /// </summary>
        public void UpdateVisual()
        {
            if (soilRenderer == null) return;

            // 土堆始终显示土壤精灵，作物图片由CropBase从CropData获取
            soilRenderer.sprite = soilSprite;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // 绘制作物位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * cropYOffset, 0.1f);
        }

        #endregion
    }
}
