using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SanitySystem
{
    /// <summary>
    /// SAN值 HUD 显示。
    /// 遵循 CurrencyUI 的 DelayedInit + 事件订阅模式。
    /// </summary>
    public class SanityUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI引用")]
        [Tooltip("SAN数值文本 (如 '85 / 100')")]
        [SerializeField] private TextMeshProUGUI sanityText;

        [Tooltip("SAN值进度条填充 Image（可选）")]
        [SerializeField] private Image sanityFillBar;

        [Header("视觉反馈")]
        [Tooltip("进度条颜色渐变（低SAN=红, 高SAN=绿）")]
        [SerializeField] private Gradient barColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.4f),
                new GradientColorKey(Color.green, 1f)
            },
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        [Header("游戏结束")]
        [Tooltip("游戏结束面板（默认隐藏）")]
        [SerializeField] private GameObject gameOverPanel;

        [Tooltip("游戏结束文案文本（可选）")]
        [SerializeField] private TextMeshProUGUI gameOverText;
        #endregion

        #region Private Fields
        private SanityManager sanityManager;
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // 确保游戏结束面板初始隐藏
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            yield return null;

            sanityManager = SanityManager.Instance;

            if (sanityManager == null)
            {
                Debug.LogError("[SanityUI] 找不到 SanityManager!");
                yield break;
            }

            // 订阅事件
            sanityManager.OnSanityChanged += OnSanityChanged;
            sanityManager.OnGameOver += OnGameOver;

            // 初始刷新
            RefreshUI(sanityManager.CurrentSanity);

            Debug.Log("[SanityUI] 初始化完成");
        }

        private void OnDestroy()
        {
            if (sanityManager != null)
            {
                sanityManager.OnSanityChanged -= OnSanityChanged;
                sanityManager.OnGameOver -= OnGameOver;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSanityChanged(float newSanity)
        {
            RefreshUI(newSanity);
        }

        private void OnGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = "迷失在了无人之境...\n\n你的理智已完全崩溃。\n主角陷入了永久的疯狂，再也无法返回现实。";
            }
        }

        #endregion

        #region Private Methods

        private void RefreshUI(float currentSanity)
        {
            if (sanityManager == null) return;

            float maxSanity = sanityManager.MaxSanity;
            float normalized = sanityManager.NormalizedSanity;

            // 更新数值文本
            if (sanityText != null)
            {
                sanityText.text = $"{Mathf.CeilToInt(currentSanity)} / {Mathf.CeilToInt(maxSanity)}";
            }

            // 更新进度条
            if (sanityFillBar != null)
            {
                sanityFillBar.fillAmount = normalized;
                sanityFillBar.color = barColorGradient.Evaluate(normalized);
            }
        }

        #endregion
    }
}
