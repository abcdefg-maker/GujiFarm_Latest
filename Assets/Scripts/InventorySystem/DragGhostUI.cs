using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// 拖拽时跟随鼠标的幽灵图标
    /// 挂在Canvas根下的一个始终存在的GameObject上
    /// 需要CanvasGroup组件（blocksRaycasts=false，防止拦截Drop事件）
    /// </summary>
    public class DragGhostUI : MonoBehaviour
    {
        public static DragGhostUI Instance { get; private set; }

        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform rectTransform;
        private Canvas rootCanvas;

        private void Awake()
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();

            // 确保有CanvasGroup
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // 自动查找子组件
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (countText == null)
                countText = GetComponentInChildren<TextMeshProUGUI>();

            Hide();
        }

        /// <summary>
        /// 显示拖拽幽灵
        /// </summary>
        public void Show(Sprite icon, int count)
        {
            gameObject.SetActive(true);

            // 确保渲染在最上层
            transform.SetAsLastSibling();

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.color = Color.white;
            }

            if (countText != null)
            {
                if (count > 1)
                {
                    countText.text = count.ToString();
                    countText.enabled = true;
                }
                else
                {
                    countText.enabled = false;
                }
            }

            // 半透明 + 不拦截射线
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.75f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 跟随指针位置
        /// </summary>
        public void FollowPointer(PointerEventData eventData)
        {
            if (rootCanvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            rectTransform.localPosition = localPoint;
        }

        /// <summary>
        /// 隐藏幽灵
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
