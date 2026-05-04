using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopSystem
{
    /// <summary>
    /// 数量选择弹窗（购买和出售共用）
    /// </summary>
    public class QuantityDialogUI : MonoBehaviour
    {
        #region Singleton
        private static QuantityDialogUI instance;
        #endregion

        #region Serialized Fields
        [Header("UI引用")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        #endregion

        #region Private Fields
        private int currentQuantity;
        private int minQuantity;
        private int maxQuantity;
        private Action<int> onConfirm;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            instance = this;

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.AddListener(OnDecreaseClicked);
            if (increaseButton != null)
                increaseButton.onClick.AddListener(OnIncreaseClicked);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            if (decreaseButton != null)
                decreaseButton.onClick.RemoveListener(OnDecreaseClicked);
            if (increaseButton != null)
                increaseButton.onClick.RemoveListener(OnIncreaseClicked);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);

            if (instance == this)
                instance = null;
        }
        #endregion

        #region Static API
        /// <summary>
        /// 显示数量选择弹窗
        /// </summary>
        public static void Show(string name, Sprite icon, int min, int max, Action<int> callback)
        {
            if (instance == null)
            {
                Debug.LogWarning("[QuantityDialogUI] 实例未找到!");
                callback?.Invoke(1);
                return;
            }

            instance.ShowDialog(name, icon, min, max, callback);
        }
        #endregion

        #region Private Methods
        private void ShowDialog(string name, Sprite icon, int min, int max, Action<int> callback)
        {
            minQuantity = min;
            maxQuantity = max;
            onConfirm = callback;
            currentQuantity = 1;

            if (itemNameText != null)
                itemNameText.text = name;

            if (itemIcon != null)
            {
                if (icon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.enabled = true;
                }
                else
                {
                    itemIcon.enabled = false;
                }
            }

            UpdateQuantityDisplay();

            if (dialogPanel != null)
                dialogPanel.SetActive(true);
        }

        private void Hide()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            onConfirm = null;
        }

        private void SetQuantity(int value)
        {
            currentQuantity = Mathf.Clamp(value, minQuantity, maxQuantity);
            UpdateQuantityDisplay();
        }

        private void UpdateQuantityDisplay()
        {
            if (quantityText != null)
                quantityText.text = currentQuantity.ToString();
        }

        private void OnDecreaseClicked()
        {
            SetQuantity(currentQuantity - 1);
        }

        private void OnIncreaseClicked()
        {
            SetQuantity(currentQuantity + 1);
        }

        private void OnConfirmClicked()
        {
            var callback = onConfirm;
            int quantity = currentQuantity;
            Hide();
            callback?.Invoke(quantity);
        }

        private void OnCancelClicked()
        {
            Hide();
        }
        #endregion
    }
}
