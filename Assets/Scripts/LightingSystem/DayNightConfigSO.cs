using UnityEngine;
using TimeSystem;

namespace LightingSystem
{
    /// <summary>
    /// 日夜光照配置数据。
    /// 所有光照参数通过 AnimationCurve 和 Gradient 定义，
    /// X轴统一使用归一化时间 (0 = 0:00, 1 = 24:00)。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDayNightConfig", menuName = "Lighting/Day Night Config")]
    public class DayNightConfigSO : ScriptableObject
    {
        [Header("目标季节")]
        [Tooltip("此配置适用的季节。默认配置设置为 Spring 即可。")]
        public Season targetSeason = Season.Spring;

        [Header("平行光 - 强度")]
        [Tooltip("X轴: 归一化时间 (0=0:00, 1=24:00). Y轴: 灯光强度 (0~1.5).")]
        public AnimationCurve lightIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.05f),       // 0:00  深夜
            new Keyframe(0.21f, 0.1f),     // 5:00  黎明
            new Keyframe(0.29f, 0.7f),     // 7:00  早晨
            new Keyframe(0.5f, 1.0f),      // 12:00 正午
            new Keyframe(0.71f, 0.6f),     // 17:00 傍晚
            new Keyframe(0.83f, 0.1f),     // 20:00 入夜
            new Keyframe(1f, 0.05f)        // 24:00 深夜
        );

        [Header("平行光 - 颜色")]
        [Tooltip("X轴: 归一化时间. 控制平行光颜色随时间变化.")]
        public Gradient lightColorGradient = CreateDefaultLightColorGradient();

        [Header("平行光 - 太阳仰角 (X轴旋转)")]
        [Tooltip("X轴: 归一化时间. Y轴: 旋转角度(度). 正值=从上方照射, 负值=地平线以下.")]
        public AnimationCurve sunPitchCurve = new AnimationCurve(
            new Keyframe(0f, -30f),        // 0:00  地平线以下
            new Keyframe(0.21f, 5f),       // 5:00  刚升起
            new Keyframe(0.29f, 30f),      // 7:00  低角度（长影子）
            new Keyframe(0.5f, 75f),       // 12:00 接近头顶（短影子）
            new Keyframe(0.71f, 20f),      // 17:00 低角度（长影子）
            new Keyframe(0.83f, -10f),     // 20:00 落下
            new Keyframe(1f, -30f)         // 24:00 地平线以下
        );

        [Header("平行光 - 水平方向 (Y轴旋转)")]
        [Tooltip("太阳的水平方向角度. 通常固定为一个值.")]
        public AnimationCurve sunYawCurve = AnimationCurve.Constant(0f, 1f, -30f);

        [Header("环境光 - 颜色")]
        [Tooltip("X轴: 归一化时间. 控制 RenderSettings.ambientLight 颜色.")]
        public Gradient ambientColorGradient = CreateDefaultAmbientColorGradient();

        [Header("环境光 - 强度")]
        [Tooltip("X轴: 归一化时间. Y轴: RenderSettings.ambientIntensity (0~1).")]
        public AnimationCurve ambientIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.08f),       // 深夜
            new Keyframe(0.21f, 0.15f),    // 黎明
            new Keyframe(0.29f, 0.5f),     // 早晨
            new Keyframe(0.5f, 0.8f),      // 正午
            new Keyframe(0.71f, 0.4f),     // 傍晚
            new Keyframe(0.83f, 0.1f),     // 入夜
            new Keyframe(1f, 0.08f)        // 深夜
        );

        [Header("屏幕遮罩 - 颜色 (含透明度)")]
        [Tooltip("X轴: 归一化时间. 控制全屏遮罩 Image 的颜色和透明度.\n白天应为完全透明, 夜晚为深色半透明.")]
        public Gradient screenOverlayGradient = CreateDefaultScreenOverlayGradient();

        [Header("夜晚程度曲线 (供效果插件使用)")]
        [Tooltip("X轴: 归一化时间. Y轴: 0=完全白天, 1=完全深夜. 用于 IDayNightEffect 读取.")]
        public AnimationCurve nightLevelCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f),        // 0:00  深夜
            new Keyframe(0.21f, 0.7f),     // 5:00  黎明
            new Keyframe(0.29f, 0.1f),     // 7:00  进入白天
            new Keyframe(0.5f, 0.0f),      // 12:00 正午
            new Keyframe(0.71f, 0.2f),     // 17:00 傍晚
            new Keyframe(0.83f, 0.9f),     // 20:00 入夜
            new Keyframe(1f, 1.0f)         // 24:00 深夜
        );

        #region Default Gradient Constructors

        private static Gradient CreateDefaultLightColorGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color32(26, 26, 64, 255), 0f),       // 0:00  深蓝月光
                    new GradientColorKey(new Color32(255, 140, 66, 255), 0.23f),  // 5:31  日出橘黄
                    new GradientColorKey(new Color32(255, 228, 181, 255), 0.30f), // 7:12  暖白早晨
                    new GradientColorKey(new Color32(255, 255, 240, 255), 0.50f), // 12:00 正午纯白
                    new GradientColorKey(new Color32(255, 228, 181, 255), 0.70f), // 16:48 暖白下午
                    new GradientColorKey(new Color32(255, 107, 53, 255), 0.80f),  // 19:12 日落橘红
                    new GradientColorKey(new Color32(43, 43, 94, 255), 0.85f),    // 20:24 入夜暗蓝
                    new GradientColorKey(new Color32(26, 26, 64, 255), 1f),       // 24:00 深蓝月光
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            return gradient;
        }

        private static Gradient CreateDefaultAmbientColorGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color32(10, 10, 32, 255), 0f),       // 深夜极暗蓝
                    new GradientColorKey(new Color32(58, 40, 32, 255), 0.25f),    // 黎明暗棕暖
                    new GradientColorKey(new Color32(128, 144, 160, 255), 0.50f), // 正午浅灰蓝
                    new GradientColorKey(new Color32(74, 48, 32, 255), 0.75f),    // 傍晚暖棕
                    new GradientColorKey(new Color32(10, 10, 32, 255), 1f),       // 深夜极暗蓝
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            return gradient;
        }

        private static Gradient CreateDefaultScreenOverlayGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color32(5, 5, 30, 255), 0f),         // 深夜深蓝
                    new GradientColorKey(new Color32(20, 15, 40, 255), 0.21f),    // 黎明
                    new GradientColorKey(new Color32(0, 0, 0, 255), 0.29f),       // 早晨（黑色但透明）
                    new GradientColorKey(new Color32(0, 0, 0, 255), 0.71f),       // 傍晚（黑色但透明）
                    new GradientColorKey(new Color32(20, 15, 40, 255), 0.83f),    // 入夜
                    new GradientColorKey(new Color32(5, 5, 30, 255), 1f),         // 深夜深蓝
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.65f, 0f),      // 0:00  深夜遮罩
                    new GradientAlphaKey(0.4f, 0.21f),    // 5:00  黎明渐亮
                    new GradientAlphaKey(0f, 0.29f),      // 7:00  白天完全透明
                    new GradientAlphaKey(0f, 0.71f),      // 17:00 傍晚仍透明
                    new GradientAlphaKey(0.5f, 0.83f),    // 20:00 入夜遮罩
                    new GradientAlphaKey(0.65f, 1f),      // 24:00 深夜遮罩
                }
            );
            return gradient;
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            ClampCurveMinimum(lightIntensityCurve, 0f);
            ClampCurveMinimum(ambientIntensityCurve, 0f);
            ClampCurveRange(nightLevelCurve, 0f, 1f);
        }

        private void ClampCurveMinimum(AnimationCurve curve, float min)
        {
            if (curve == null) return;
            for (int i = 0; i < curve.length; i++)
            {
                var key = curve[i];
                if (key.value < min)
                {
                    key.value = min;
                    curve.MoveKey(i, key);
                }
            }
        }

        private void ClampCurveRange(AnimationCurve curve, float min, float max)
        {
            if (curve == null) return;
            for (int i = 0; i < curve.length; i++)
            {
                var key = curve[i];
                if (key.value < min || key.value > max)
                {
                    key.value = Mathf.Clamp(key.value, min, max);
                    curve.MoveKey(i, key);
                }
            }
        }

        #endregion
    }
}
