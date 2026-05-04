using UnityEngine;
using TimeSystem;

namespace FarmingSystem
{
    /// <summary>
    /// 作物配置数据 - ScriptableObject
    /// 定义作物的生长参数、视觉表现和收获配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewCropData", menuName = "Farming/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("作物唯一标识")]
        public string cropID = "crop_001";

        [Tooltip("作物显示名称")]
        public string cropName = "New Crop";

        [Tooltip("作物描述")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("生长配置")]
        [Tooltip("每个生长阶段所需的天数")]
        public int daysPerStage = 1;

        [Tooltip("浇水冷却时间（真实秒数）")]
        public float waterCooldown = 1f;

        [Tooltip("每次浇水加速的阶段数")]
        public int waterSpeedUpStages = 1;

        [Header("视觉表现")]
        [Tooltip("各生长阶段的精灵图 (5个: Seed, Sprout, Growing, Mature, Harvestable)")]
        public Sprite[] stageSprites = new Sprite[5];

        [Header("收获配置")]
        [Tooltip("收获物品的Prefab")]
        public GameObject harvestItemPrefab;

        [Tooltip("最小收获数量")]
        [Min(1)]
        public int minHarvestAmount = 1;

        [Tooltip("最大收获数量")]
        [Min(1)]
        public int maxHarvestAmount = 3;

        [Tooltip("种子物品的Prefab（用于种子掉落）")]
        public GameObject seedItemPrefab;

        [Header("季节限制（可选）")]
        [Tooltip("允许种植的季节，为空则全季节可种")]
        public Season[] allowedSeasons;

        [Header("变异配置")]
        [Tooltip("是否为变异作物")]
        public bool isMutated = false;

        [Tooltip("如果是变异作物，引用其正常版本")]
        public CropData normalVariant;

        #region Public Methods

        /// <summary>
        /// 获取指定阶段的精灵图
        /// </summary>
        public Sprite GetStageSprite(GrowthStage stage)
        {
            int index = (int)stage;
            if (stageSprites != null && index >= 0 && index < stageSprites.Length)
            {
                return stageSprites[index];
            }
            return null;
        }

        /// <summary>
        /// 检查当前季节是否允许种植
        /// </summary>
        public bool CanPlantInSeason(Season season)
        {
            if (allowedSeasons == null || allowedSeasons.Length == 0)
            {
                return true; // 没有限制，全季节可种
            }

            foreach (Season s in allowedSeasons)
            {
                if (s == season) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取随机收获数量
        /// </summary>
        public int GetRandomHarvestAmount()
        {
            return Random.Range(minHarvestAmount, maxHarvestAmount + 1);
        }

        /// <summary>
        /// 获取总生长天数（从种子到可收获）
        /// </summary>
        public int GetTotalGrowthDays()
        {
            // 4个阶段转换：Seed->Sprout->Growing->Mature->Harvestable
            return daysPerStage * 4;
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            // 确保最大收获数量不小于最小收获数量
            if (maxHarvestAmount < minHarvestAmount)
            {
                maxHarvestAmount = minHarvestAmount;
            }

            // 确保精灵数组长度为5
            if (stageSprites == null || stageSprites.Length != 5)
            {
                System.Array.Resize(ref stageSprites, 5);
            }
        }

        #endregion
    }
}
