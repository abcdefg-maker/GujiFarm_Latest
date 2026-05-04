using System;
using UnityEngine;

namespace TimeSystem
{
    /// <summary>
    /// 时间管理器 - 管理游戏内的时间流逝、季节变化
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("时间设置")]
        [SerializeField] private float dayLengthInSeconds = 300f;  // 真实5分钟 = 游戏内1天
        [SerializeField] private int daysPerSeason = 16;           // 每季16天
        [SerializeField] private float startHour = 6f;             // 游戏开始时的小时

        [Header("当前状态")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private float currentHour;
        [SerializeField] private Season currentSeason = Season.Spring;
        [SerializeField] private DayPhase currentPhase = DayPhase.Dawn;
        [SerializeField] private bool isPaused = false;

        // 公开属性
        public int CurrentDay => currentDay;
        public float CurrentHour => currentHour;
        public Season CurrentSeason => currentSeason;
        public DayPhase CurrentPhase => currentPhase;
        public bool IsPaused => isPaused;

        /// <summary>
        /// 当前小时内的分钟数 (0-60)，用于模拟时钟分针
        /// </summary>
        public float CurrentMinute => (hourLength > 0f) ? (hourTimer / hourLength) * 60f : 0f;

        /// <summary>
        /// 获取当前季节内的第几天 (1-16)
        /// </summary>
        public int DayInSeason => ((currentDay - 1) % daysPerSeason) + 1;

        // 事件
        public event Action<float> OnHourChanged;       // 每小时变化
        public event Action<DayPhase> OnPhaseChanged;   // 时段变化
        public event Action<int> OnDayChanged;          // 每天变化
        public event Action<Season> OnSeasonChanged;    // 季节变化

        // 内部计时
        private float hourTimer = 0f;
        private float hourLength;  // 一小时的真实时长

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
        }

        private void Start()
        {
            // 计算每小时的真实时长
            hourLength = dayLengthInSeconds / 24f;

            // 初始化时间
            currentHour = startHour;
            currentPhase = GetPhaseFromHour(currentHour);

            Debug.Log($"[TimeManager] 初始化完成 - 第{currentDay}天 {currentSeason} {currentHour:F0}:00 ({GetPhaseName()})");
        }

        private void Update()
        {
            if (isPaused) return;

            hourTimer += Time.deltaTime;

            if (hourTimer >= hourLength)
            {
                hourTimer -= hourLength;
                AdvanceHour();
            }
        }

        /// <summary>
        /// 推进一小时
        /// </summary>
        private void AdvanceHour()
        {
            currentHour += 1f;

            // 检查时段变化
            DayPhase newPhase = GetPhaseFromHour(currentHour);
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
                Debug.Log($"[TimeManager] 时段变化: {GetPhaseName()}");
            }

            OnHourChanged?.Invoke(currentHour);

            // 检查是否到了新的一天
            if (currentHour >= 24f)
            {
                currentHour = 0f;
                AdvanceDay();
            }
        }

        /// <summary>
        /// 推进一天
        /// </summary>
        private void AdvanceDay()
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
            Debug.Log($"[TimeManager] 新的一天: 第{currentDay}天 ({GetSeasonName()} 第{DayInSeason}天)");

            // 检查季节变化 (每16天换季)
            if ((currentDay - 1) % daysPerSeason == 0 && currentDay > 1)
            {
                AdvanceSeason();
            }
        }

        /// <summary>
        /// 推进季节
        /// </summary>
        private void AdvanceSeason()
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            OnSeasonChanged?.Invoke(currentSeason);
            Debug.Log($"[TimeManager] 季节变化: {GetSeasonName()}");
        }

        /// <summary>
        /// 根据小时获取时段
        /// </summary>
        public DayPhase GetPhaseFromHour(float hour)
        {
            if (hour >= 5f && hour < 7f) return DayPhase.Dawn;
            if (hour >= 7f && hour < 12f) return DayPhase.Morning;
            if (hour >= 12f && hour < 17f) return DayPhase.Afternoon;
            if (hour >= 17f && hour < 20f) return DayPhase.Evening;
            return DayPhase.Night;
        }

        #region 公开方法

        /// <summary>
        /// 暂停时间
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            Debug.Log("[TimeManager] 时间暂停");
        }

        /// <summary>
        /// 恢复时间
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            Debug.Log("[TimeManager] 时间恢复");
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// 跳到下一天（用于睡眠/调试）
        /// </summary>
        public void SkipToNextDay()
        {
            currentHour = startHour;
            hourTimer = 0f;
            AdvanceDay();

            // 更新时段
            DayPhase newPhase = GetPhaseFromHour(currentHour);
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
            }

            OnHourChanged?.Invoke(currentHour);
        }

        /// <summary>
        /// 跳过指定小时数
        /// </summary>
        public void SkipHours(float hours)
        {
            for (int i = 0; i < Mathf.FloorToInt(hours); i++)
            {
                AdvanceHour();
            }
        }

        /// <summary>
        /// 获取季节中文名
        /// </summary>
        public string GetSeasonName()
        {
            return currentSeason switch
            {
                Season.Spring => "Spring",
                Season.Summer => "Summer",
                Season.Autumn => "Autumn",
                Season.Winter => "Winter",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 获取时段中文名
        /// </summary>
        public string GetPhaseName()
        {
            return currentPhase switch
            {
                DayPhase.Dawn => "Dawn",
                DayPhase.Morning => "Morning",
                DayPhase.Afternoon => "Afternoon",
                DayPhase.Evening => "Evening",
                DayPhase.Night => "Night",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 获取格式化的时间字符串 (HH:00)
        /// </summary>
        public string GetFormattedTime()
        {
            int hour = Mathf.FloorToInt(currentHour);
            return $"{hour:D2}:00";
        }

        /// <summary>
        /// 获取格式化的日期字符串
        /// </summary>
        public string GetFormattedDate()
        {
            return $"the {currentDay} day";
        }

        /// <summary>
        /// 获取格式化的季节日期字符串
        /// </summary>
        public string GetFormattedSeasonDate()
        {
            return $"{GetSeasonName()} the {DayInSeason} day";
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 设置时间倍速（调试用）
        /// </summary>
        public void SetTimeScale(float scale)
        {
            hourLength = (dayLengthInSeconds / 24f) / scale;
            Debug.Log($"[TimeManager] 时间倍速设置为 {scale}x");
        }

        #endregion
    }
}
