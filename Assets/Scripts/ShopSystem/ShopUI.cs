using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShopSystem
{
    /// <summary>
    /// 商店根面板控制器（开关 + ESC关闭）
    ///
    /// 重要：此脚本必须挂在一个始终激活的父物体上（如ShopRoot），
    /// shopPanel引用指向子面板。不要挂在shopPanel自身上，
    /// 否则SetActive(false)后Update不再运行，无法检测ESC按键。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("面板引用")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private ShopBuyPanelUI buyPanel;
        [SerializeField] private ShopSellPanelUI sellPanel;

        [Header("顶部")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI shopTitleText;
        #endregion

        #region Private Fields
        private ShopManager shopManager;
        private bool initialized;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 初始隐藏
            if (shopPanel != null)
                shopPanel.SetActive(false);

            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            shopManager = ShopManager.Instance;

            if (shopManager == null)
            {
                Debug.LogError("[ShopUI] 找不到 ShopManager!");
                yield break;
            }

            shopManager.OnShopOpened += OnShopOpened;
            shopManager.OnShopClosed += OnShopClosed;
            shopManager.OnTransactionFailed += OnTransactionFailed;

            // 如果序列化引用丢失，自动在子层级中查找CloseButton
            if (closeButton == null && shopPanel != null)
            {
                Transform closeBtnTransform = shopPanel.transform.Find("CloseButton");
                if (closeBtnTransform != null)
                    closeButton = closeBtnTransform.GetComponent<Button>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogWarning("[ShopUI] closeButton 引用为空，无法绑定关闭事件!");
            }

            initialized = true;
            Debug.Log("[ShopUI] 初始化完成");
        }

        private void Update()
        {
            if (!initialized) return;

            // ESC关闭商店
            if (shopManager != null && shopManager.IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                shopManager.CloseShop();
            }
        }

        private void OnDestroy()
        {
            if (shopManager != null)
            {
                shopManager.OnShopOpened -= OnShopOpened;
                shopManager.OnShopClosed -= OnShopClosed;
                shopManager.OnTransactionFailed -= OnTransactionFailed;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }
        #endregion

        #region Event Callbacks
        private void OnShopOpened()
        {
            if (shopManager.Catalog != null && shopTitleText != null)
            {
                shopTitleText.text = shopManager.Catalog.shopName;
            }

            Show();

            if (buyPanel != null)
                buyPanel.RefreshUI();

            if (sellPanel != null)
                sellPanel.RefreshUI();
        }

        private void OnShopClosed()
        {
            Hide();
        }

        private void OnTransactionFailed(string message)
        {
            Debug.Log($"[ShopUI] 交易失败: {message}");
            // TODO: 显示提示消息UI
        }
        #endregion

        #region Private Methods
        private void Show()
        {
            if (shopPanel != null)
                shopPanel.SetActive(true);
        }

        private void Hide()
        {
            if (shopPanel != null)
                shopPanel.SetActive(false);
        }

        private void OnCloseButtonClicked()
        {
            if (shopManager != null)
                shopManager.CloseShop();
        }
        #endregion
    }
}
