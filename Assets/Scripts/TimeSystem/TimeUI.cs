using UnityEngine;
using TMPro;

namespace TimeSystem
{
    /// <summary>
    /// 时间UI控制器 - 显示当前时间、日期、季节、时段
    /// </summary>
    public class TimeUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private TimeManager timeManager;

        [Header("UI元素")]
        [SerializeField] private TextMeshProUGUI dayText;           // 显示"第 X 天"
        [SerializeField] private TextMeshProUGUI seasonText;        // 显示"春季 第X天"
        [SerializeField] private TextMeshProUGUI timeText;          // 显示"12:00"
        [SerializeField] private TextMeshProUGUI phaseText;         // 显示"下午"

        [Header("可选UI元素")]
        [SerializeField] private GameObject pauseIndicator;         // 暂停指示器

        private void Start()
        {
            // 延迟初始化，确保 TimeManager 已经初始化
            StartCoroutine(DelayedInit());
        }

        private System.Collections.IEnumerator DelayedInit()
        {
            // 等待一帧，确保其他脚本的 Awake/Start 已执行
            yield return null;

            // 自动查找TimeManager
            if (timeManager == null)
            {
                timeManager = TimeManager.Instance;
            }

            if (timeManager == null)
            {
                Debug.LogError("TimeUI: 找不到 TimeManager!");
                yield break;
            }

            Debug.Log("TimeUI: 找到 TimeManager，开始初始化");

            // 订阅事件
            timeManager.OnHourChanged += OnHourChanged;
            timeManager.OnPhaseChanged += OnPhaseChanged;
            timeManager.OnDayChanged += OnDayChanged;
            timeManager.OnSeasonChanged += OnSeasonChanged;

            // 初始刷新
            RefreshUI();

            Debug.Log("TimeUI: 初始化完成");
        }

        private void OnDestroy()
        {
            if (timeManager != null)
            {
                timeManager.OnHourChanged -= OnHourChanged;
                timeManager.OnPhaseChanged -= OnPhaseChanged;
                timeManager.OnDayChanged -= OnDayChanged;
                timeManager.OnSeasonChanged -= OnSeasonChanged;
            }
        }

        /// <summary>
        /// 刷新所有UI
        /// </summary>
        public void RefreshUI()
        {
            if (timeManager == null) return;

            UpdateDayText();
            UpdateSeasonText();
            UpdateTimeText();
            UpdatePhaseText();
            UpdatePauseIndicator();
        }

        /// <summary>
        /// 更新天数显示
        /// </summary>
        private void UpdateDayText()
        {
            if (dayText != null)
            {
                dayText.text = timeManager.GetFormattedDate();
            }
        }

        /// <summary>
        /// 更新季节显示
        /// </summary>
        private void UpdateSeasonText()
        {
            if (seasonText != null)
            {
                seasonText.text = timeManager.GetFormattedSeasonDate();
            }
        }

        /// <summary>
        /// 更新时间显示
        /// </summary>
        private void UpdateTimeText()
        {
            if (timeText != null)
            {
                timeText.text = timeManager.GetFormattedTime();
            }
        }

        /// <summary>
        /// 更新时段显示
        /// </summary>
        private void UpdatePhaseText()
        {
            if (phaseText != null)
            {
                phaseText.text = timeManager.GetPhaseName();
            }
        }

        /// <summary>
        /// 更新暂停指示器
        /// </summary>
        private void UpdatePauseIndicator()
        {
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(timeManager.IsPaused);
            }
        }

        #region 事件回调

        /// <summary>
        /// 小时变化回调
        /// </summary>
        private void OnHourChanged(float hour)
        {
            UpdateTimeText();
        }

        /// <summary>
        /// 时段变化回调
        /// </summary>
        private void OnPhaseChanged(DayPhase phase)
        {
            UpdatePhaseText();
        }

        /// <summary>
        /// 天数变化回调
        /// </summary>
        private void OnDayChanged(int day)
        {
            UpdateDayText();
            UpdateSeasonText();  // 季节内天数也会变化
        }

        /// <summary>
        /// 季节变化回调
        /// </summary>
        private void OnSeasonChanged(Season season)
        {
            UpdateSeasonText();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 显示时间UI
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏时间UI
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 当暂停状态改变时调用（需要手动调用或订阅暂停事件）
        /// </summary>
        public void OnPauseStateChanged()
        {
            UpdatePauseIndicator();
        }

        #endregion
    }
}
