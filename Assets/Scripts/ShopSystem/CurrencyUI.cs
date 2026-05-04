using System.Collections;
using UnityEngine;
using TMPro;

namespace ShopSystem
{
    /// <summary>
    /// HUD金币常驻显示（右上角）
    /// </summary>
    public class CurrencyUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI引用")]
        [SerializeField] private TextMeshProUGUI goldText;
        #endregion

        #region Private Fields
        private CurrencyManager currencyManager;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            currencyManager = CurrencyManager.Instance;

            if (currencyManager == null)
            {
                Debug.LogError("[CurrencyUI] 找不到 CurrencyManager!");
                yield break;
            }

            currencyManager.OnGoldChanged += OnGoldChanged;
            RefreshUI();

            Debug.Log("[CurrencyUI] 初始化完成");
        }

        private void OnDestroy()
        {
            if (currencyManager != null)
            {
                currencyManager.OnGoldChanged -= OnGoldChanged;
            }
        }
        #endregion

        #region Private Methods
        private void OnGoldChanged(int newGold)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (goldText != null && currencyManager != null)
            {
                goldText.text = currencyManager.CurrentGold.ToString();
            }
        }
        #endregion
    }
}
