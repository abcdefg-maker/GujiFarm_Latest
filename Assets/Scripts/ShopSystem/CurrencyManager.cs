using System;
using UnityEngine;
using Core.Constants;

namespace ShopSystem
{
    /// <summary>
    /// 金币管理器单例
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        #region Singleton
        public static CurrencyManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("金币设置")]
        [SerializeField] private int startingGold = GameConstants.DefaultStartingGold;
        #endregion

        #region Private Fields
        private int currentGold;
        #endregion

        #region Public Properties
        public int CurrentGold => currentGold;
        #endregion

        #region Events
        public event Action<int> OnGoldChanged;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            currentGold = startingGold;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 增加金币
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
            Debug.Log($"[CurrencyManager] 获得 {amount} 金币，当前: {currentGold}");
        }

        /// <summary>
        /// 消费金币
        /// </summary>
        /// <returns>是否成功消费</returns>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return false;
            if (currentGold < amount) return false;

            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            Debug.Log($"[CurrencyManager] 消费 {amount} 金币，剩余: {currentGold}");
            return true;
        }

        /// <summary>
        /// 检查是否有足够金币
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return currentGold >= amount;
        }
        #endregion
    }
}
