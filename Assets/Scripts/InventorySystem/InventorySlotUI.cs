using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// 单个槽位UI组件
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Image backgroundImage;

        [Header("颜色设置")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(0.4f, 0.6f, 0.8f, 0.9f);

        private int slotIndex;
        public int SlotIndex => slotIndex;

        /// <summary>
        /// 初始化槽位UI
        /// </summary>
        public void Initialize(int index)
        {
            slotIndex = index;

            // 自动查找子组件（如果未手动设置）
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (highlightImage == null)
                highlightImage = transform.Find("Highlight")?.GetComponent<Image>();
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            if (countText == null)
                countText = GetComponentInChildren<TMPro.TextMeshProUGUI>();

            // 调试日志
            Debug.Log($"SlotUI {index} 初始化: Icon={iconImage != null}, Highlight={highlightImage != null}, BG={backgroundImage != null}");

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

            Debug.Log($"SlotUI {slotIndex}: 设置物品 {slot.itemRef?.itemName}, 图标={slot.itemRef?.itemIcon != null}");

            // 显示图标
            if (iconImage != null)
            {
                if (slot.itemRef != null && slot.itemRef.itemIcon != null)
                {
                    iconImage.sprite = slot.itemRef.itemIcon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white; // 确保颜色可见
                    Debug.Log($"SlotUI {slotIndex}: 图标已设置");
                }
                else
                {
                    iconImage.enabled = false;
                    Debug.Log($"SlotUI {slotIndex}: 物品没有图标");
                }
            }
            else
            {
                Debug.LogWarning($"SlotUI {slotIndex}: iconImage 为空!");
            }

            // 显示数量（大于1才显示）
            if (countText != null)
            {
                if (slot.count > 1)
                {
                    countText.text = slot.count.ToString();
                    countText.enabled = true;
                }
                else
                {
                    countText.enabled = false;
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
            else
            {
                Debug.LogWarning($"SlotUI {slotIndex}: backgroundImage 为空，无法显示高亮!");
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
                countText.enabled = false;
            }
        }
    }
}
