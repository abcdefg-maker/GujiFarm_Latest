using UnityEngine;
using TimeSystem;

namespace FarmingSystem
{
    /// <summary>
    /// 作物核心逻辑 - 管理生长、浇水、收获
    /// 订阅TimeManager的OnDayChanged事件实现每日生长
    /// </summary>
    public class CropBase : MonoBehaviour
    {
        [Header("当前状态")]
        [SerializeField] private GrowthStage currentStage = GrowthStage.Seed;
        [SerializeField] private int daysInCurrentStage = 0;

        [Header("浇水状态")]
        [SerializeField] private float waterCooldownTimer = 0f;
        [SerializeField] private bool canBeWatered = true;

        // 配置数据
        private CropData cropData;
        private SpriteRenderer spriteRenderer;
        private SoilMound parentMound;

        #region Public Properties

        public GrowthStage CurrentStage => currentStage;
        public CropData CropData => cropData;
        public bool CanBeWatered => canBeWatered;
        public bool IsHarvestable => currentStage == GrowthStage.Harvestable;

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化作物（由SoilMound调用）
        /// </summary>
        public void Initialize(CropData data, SoilMound mound, SpriteRenderer renderer)
        {
            cropData = data;
            parentMound = mound;
            spriteRenderer = renderer;

            currentStage = GrowthStage.Seed;
            daysInCurrentStage = 0;
            waterCooldownTimer = 0f;
            canBeWatered = true;

            // 订阅时间事件
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += OnDayChanged;
            }

            UpdateVisual();
            Debug.Log($"[CropBase] {cropData.cropName} 已种植");
        }

        private void OnDestroy()
        {
            // 取消订阅，防止内存泄漏
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        #endregion

        #region Unity Update

        private void Update()
        {
            // 处理浇水冷却
            if (!canBeWatered && waterCooldownTimer > 0f)
            {
                waterCooldownTimer -= Time.deltaTime;
                if (waterCooldownTimer <= 0f)
                {
                    canBeWatered = true;
                    waterCooldownTimer = 0f;
                    Debug.Log($"[CropBase] {cropData.cropName} 浇水冷却结束，可以再次浇水");
                }
            }
        }

        #endregion

        #region Growth Logic

        /// <summary>
        /// 每天变化时的处理
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            if (currentStage == GrowthStage.Harvestable)
            {
                return; // 已成熟，不再生长
            }

            ProcessGrowth();
        }

        /// <summary>
        /// 处理生长
        /// </summary>
        private void ProcessGrowth()
        {
            daysInCurrentStage++;

            // 检查是否进入下一阶段
            if (daysInCurrentStage >= cropData.daysPerStage)
            {
                AdvanceStage();
            }

            Debug.Log($"[CropBase] {cropData.cropName} - 阶段:{currentStage}, 天数:{daysInCurrentStage}/{cropData.daysPerStage}");
        }

        /// <summary>
        /// 进入下一生长阶段
        /// </summary>
        private void AdvanceStage()
        {
            if (currentStage < GrowthStage.Harvestable)
            {
                currentStage++;
                daysInCurrentStage = 0;
                UpdateVisual();

                Debug.Log($"[CropBase] {cropData.cropName} 进入 {currentStage} 阶段");

                if (currentStage == GrowthStage.Harvestable)
                {
                    Debug.Log($"[CropBase] {cropData.cropName} 已成熟，可以收获！");
                }
            }
        }

        #endregion

        #region Watering

        /// <summary>
        /// 浇水 - 加速生长
        /// </summary>
        /// <returns>是否成功浇水</returns>
        public bool Water()
        {
            if (!canBeWatered)
            {
                Debug.Log($"[CropBase] {cropData.cropName} 浇水冷却中，还需 {waterCooldownTimer:F1} 秒");
                return false;
            }

            if (currentStage == GrowthStage.Harvestable)
            {
                Debug.Log($"[CropBase] {cropData.cropName} 已成熟，无需浇水");
                return false;
            }

            // 执行浇水加速
            ApplyWaterBoost();

            // 进入冷却
            canBeWatered = false;
            waterCooldownTimer = cropData.waterCooldown;

            Debug.Log($"[CropBase] {cropData.cropName} 浇水成功，冷却 {cropData.waterCooldown} 秒");
            return true;
        }

        /// <summary>
        /// 应用浇水加速效果
        /// </summary>
        private void ApplyWaterBoost()
        {
            int stagesToAdvance = cropData.waterSpeedUpStages;

            for (int i = 0; i < stagesToAdvance; i++)
            {
                if (currentStage >= GrowthStage.Harvestable)
                {
                    break;
                }

                AdvanceStage();
            }

            UpdateVisual();
        }

        #endregion

        #region Harvest

        /// <summary>
        /// 收获作物
        /// </summary>
        /// <param name="harvester">收获者</param>
        public void Harvest(GameObject harvester)
        {
            if (currentStage != GrowthStage.Harvestable)
            {
                Debug.Log($"[CropBase] {cropData.cropName} 尚未成熟，无法收获");
                return;
            }

            // 计算收获数量
            int amount = cropData.GetRandomHarvestAmount();

            // 生成收获物品
            SpawnHarvestItems(amount);

            Debug.Log($"[CropBase] {harvester.name} 收获了 {amount} 个 {cropData.cropName}");
        }

        /// <summary>
        /// 生成收获物品
        /// </summary>
        private void SpawnHarvestItems(int amount)
        {
            if (cropData.harvestItemPrefab == null)
            {
                Debug.LogError($"[CropBase] {cropData.cropName} 未设置收获物品Prefab!");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                // 随机偏移位置
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    0.5f,
                    Random.Range(-0.3f, 0.3f)
                );

                Vector3 spawnPosition = transform.position + spawnOffset;

                // 实例化物品
                GameObject itemObj = Instantiate(
                    cropData.harvestItemPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

                // 添加一点向上的力让物品弹出
                if (itemObj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.AddForce(Vector3.up * 2f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
                }
            }
        }

        #endregion

        #region Visual

        /// <summary>
        /// 更新作物视觉
        /// </summary>
        private void UpdateVisual()
        {
            if (spriteRenderer == null || cropData == null)
            {
                return;
            }

            Sprite stageSprite = cropData.GetStageSprite(currentStage);
            if (stageSprite != null)
            {
                spriteRenderer.sprite = stageSprite;
            }
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 强制进入下一阶段（调试用）
        /// </summary>
        [ContextMenu("Debug: Advance Stage")]
        public void DebugAdvanceStage()
        {
            if (currentStage < GrowthStage.Harvestable)
            {
                AdvanceStage();
            }
        }

        /// <summary>
        /// 强制成熟（调试用）
        /// </summary>
        [ContextMenu("Debug: Set Harvestable")]
        public void DebugSetHarvestable()
        {
            currentStage = GrowthStage.Harvestable;
            daysInCurrentStage = 0;
            UpdateVisual();
            Debug.Log($"[CropBase] {cropData.cropName} 已设置为可收获状态");
        }

        #endregion
    }
}
