using UnityEngine;

namespace TimeSystem
{
    /// <summary>
    /// 模拟时钟UI - 驱动时针和分针根据游戏时间旋转
    /// 在Unity中将时钟表盘、时针、分针、中心点作为UI Image配置到对应字段即可
    /// </summary>
    public class ClockUI : MonoBehaviour
    {
        [Header("指针引用")]
        [SerializeField] private RectTransform hourHand;      // 时针 (shizhen.png)
        [SerializeField] private RectTransform minuteHand;    // 分针 (fenzhen.png)

        [Header("设置")]
        [Tooltip("启用平滑旋转，关闭则为离散跳动")]
        [SerializeField] private bool smoothRotation = true;
        [Tooltip("平滑旋转的插值速度")]
        [SerializeField] private float smoothSpeed = 5f;

        private float targetHourAngle;
        private float targetMinuteAngle;
        private float currentHourAngle;
        private float currentMinuteAngle;

        private void Update()
        {
            if (TimeManager.Instance == null) return;

            float hour = TimeManager.Instance.CurrentHour;
            float minute = TimeManager.Instance.CurrentMinute;

            // 时针: 12小时一圈, 每小时30度, 分针贡献额外0.5度/分钟
            targetHourAngle = (hour % 12f) * 30f + minute * 0.5f;

            // 分针: 60分钟一圈, 每分钟6度
            targetMinuteAngle = minute * 6f;

            if (smoothRotation)
            {
                float t = smoothSpeed * Time.deltaTime;
                currentHourAngle = Mathf.LerpAngle(currentHourAngle, targetHourAngle, t);
                currentMinuteAngle = Mathf.LerpAngle(currentMinuteAngle, targetMinuteAngle, t);
            }
            else
            {
                currentHourAngle = targetHourAngle;
                currentMinuteAngle = targetMinuteAngle;
            }

            // Z轴负方向 = 顺时针旋转
            if (hourHand != null)
                hourHand.localRotation = Quaternion.Euler(0f, 0f, -currentHourAngle);

            if (minuteHand != null)
                minuteHand.localRotation = Quaternion.Euler(0f, 0f, -currentMinuteAngle);
        }

        /// <summary>
        /// 立即同步指针到当前时间（跳过平滑过渡）
        /// </summary>
        public void SyncImmediate()
        {
            if (TimeManager.Instance == null) return;

            float hour = TimeManager.Instance.CurrentHour;
            float minute = TimeManager.Instance.CurrentMinute;

            currentHourAngle = (hour % 12f) * 30f + minute * 0.5f;
            currentMinuteAngle = minute * 6f;

            targetHourAngle = currentHourAngle;
            targetMinuteAngle = currentMinuteAngle;
        }
    }
}
