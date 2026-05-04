using UnityEngine;
using Core.Items;

namespace FarmingSystem
{
    /// <summary>
    /// 收获物品 - 作物收获后产出的物品
    /// 继承自Item基类，设置为Crop类型
    /// </summary>
    public class CropItem : Item
    {
        [Header("作物信息")]
        [Tooltip("关联的作物数据（用于显示信息）")]
        [SerializeField] private CropData sourceCropData;

        [Header("使用属性")]
        [Tooltip("是否可食用")]
        [SerializeField] private bool isEdible = false;

        [Tooltip("食用恢复的体力值")]
        [SerializeField] private int staminaRestore = 0;

        #region Public Properties

        public CropData SourceCropData => sourceCropData;
        public int SellPrice => base.sellPrice;
        public bool IsEdible => isEdible;
        public int StaminaRestore => staminaRestore;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // 确保物品类型为Crop
            itemType = ItemType.Crop;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 使用物品（如果可食用）
        /// </summary>
        /// <param name="user">使用者</param>
        /// <returns>是否成功使用</returns>
        public virtual bool Use(GameObject user)
        {
            if (!isEdible)
            {
                Debug.Log($"[CropItem] {itemName} 不可食用");
                return false;
            }

            // TODO: 实现体力恢复系统后，在此处调用
            Debug.Log($"[CropItem] {user.name} 食用了 {itemName}，恢复 {staminaRestore} 体力");

            return true;
        }

        /// <summary>
        /// 出售物品
        /// </summary>
        /// <returns>出售获得的金币</returns>
        public virtual int Sell()
        {
            Debug.Log($"[CropItem] 出售 {itemName}，获得 {base.sellPrice} 金币");
            return base.sellPrice;
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            // 确保物品类型正确
            if (itemType != ItemType.Crop)
            {
                itemType = ItemType.Crop;
            }

            // 如果有关联的作物数据，自动设置名称
            if (sourceCropData != null && string.IsNullOrEmpty(itemName))
            {
                itemName = sourceCropData.cropName;
            }
        }

        #endregion
    }
}
