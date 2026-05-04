using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopSystem
{
    /// <summary>
    /// 商品卡片UI（左侧购买面板中的单个商品）
    /// 布局: [图标] [名称] / [金币图标] [价格数字]
    /// </summary>
    public class ShopItemSlotUI : MonoBehaviour
    {
        #region UI References
        [Header("UI引用")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        #endregion

        #region Private Fields
        private ShopItemEntry entry;
        #endregion

        #region Events
        public event Action<ShopItemEntry> OnBuyClicked;
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化，自动查找子组件
        /// </summary>
        public void Initialize()
        {
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (nameText == null)
                nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (coinIcon == null)
                coinIcon = transform.Find("CoinIcon")?.GetComponent<Image>();
            if (priceText == null)
                priceText = transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
            if (buyButton == null)
                buyButton = GetComponent<Button>();

            if (buyButton != null)
            {
                buyButton.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// 设置商品数据
        /// </summary>
        public void Setup(ShopItemEntry shopEntry)
        {
            entry = shopEntry;

            if (iconImage != null)
            {
                Sprite icon = entry.GetIcon();
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            if (nameText != null)
            {
                nameText.text = entry.GetDisplayName();
            }

            if (priceText != null)
            {
                priceText.text = entry.buyPrice.ToString();
            }
        }
        #endregion

        #region Private Methods
        private void OnButtonClicked()
        {
            if (entry != null)
            {
                OnBuyClicked?.Invoke(entry);
            }
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(OnButtonClicked);
            }
        }
        #endregion
    }
}
