using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using InventorySystem;

namespace ShopSystem
{
    /// <summary>
    /// 商店/背包中的物品栏槽位UI（可点击选中）
    /// 实现IPointerClickHandler以接收子物体冒泡上来的点击事件
    /// </summary>
    public class ShopInventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        #region UI References
        [Header("UI引用")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button slotButton;

        [Header("颜色设置")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(0.4f, 0.6f, 0.8f, 0.9f);
        #endregion

        #region Private Fields
        private int slotIndex;
        #endregion

        #region Public Properties
        public int SlotIndex => slotIndex;
        #endregion

        #region Events
        public event Action<int> OnSlotClicked;
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化槽位UI
        /// </summary>
        public void Initialize(int index)
        {
            slotIndex = index;

            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (highlightImage == null)
                highlightImage = transform.Find("Highlight")?.GetComponent<Image>();
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            if (countText == null)
                countText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (slotButton == null)
                slotButton = GetComponent<Button>();
            if (slotButton == null)
                slotButton = GetComponentInChildren<Button>(true);

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnButtonClicked);
            }

            Clear();
        }

        /// <summary>
        /// 设置槽位数据
        /// </summary>
        public void SetSlot(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                Clear();
                return;
            }

            // 显示图标
            if (iconImage != null)
            {
                if (slot.itemRef != null && slot.itemRef.itemIcon != null)
                {
                    iconImage.sprite = slot.itemRef.itemIcon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            // 显示数量（大于1才显示）
            if (countText != null)
            {
                if (slot.count > 1)
                {
                    countText.text = slot.count.ToString();
                    countText.gameObject.SetActive(true);
                }
                else
                {
                    countText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = isHighlighted;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isHighlighted ? highlightColor : normalColor;
            }
        }

        /// <summary>
        /// 清空显示
        /// </summary>
        public void Clear()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
            {
                countText.text = "";
                countText.gameObject.SetActive(false);
            }

            SetHighlight(false);
        }
        #endregion

        #region IPointerClickHandler
        /// <summary>
        /// 接收点击事件（子物体Icon等的点击会冒泡到此处）
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
        #endregion

        #region Private Methods
        private void OnButtonClicked()
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnButtonClicked);
            }
        }
        #endregion
    }
}
