using UnityEngine;
using Core.Items;

namespace ItemSystem.Effects
{
    /// <summary>
    /// 效果：在拾取时播放音效
    ///
    /// 功能：
    /// - 在拾取者的位置播放指定的音频片段
    /// - 可配置音量大小
    /// - 使用AudioSource.PlayClipAtPoint实现3D空间音效
    ///
    /// 配置参数：
    /// - pickupSound: 要播放的音频片段（AudioClip）
    /// - volume: 音量大小（0.0 - 1.0）
    ///
    /// 使用场景：
    /// - 拾取物品时提供音频反馈
    /// - 不同物品类型可配置不同的拾取音效
    /// - 增强玩家的操作体验和沉浸感
    ///
    /// 技术细节：
    /// - 使用PlayClipAtPoint创建临时AudioSource
    /// - 音效播放完毕后自动销毁
    /// - 支持3D空间音效（距离衰减）
    ///
    /// 创建方式：
    /// Unity编辑器 > 右键 > Create > Pickup/Effects/Play Sound
    /// </summary>
    [CreateAssetMenu(fileName = "PlaySound", menuName = "Pickup/Effects/Play Sound")]
    public class PlaySoundEffect : ScriptablePickupEffect
    {
        [Tooltip("拾取时播放的音频片段")]
        public AudioClip pickupSound;

        [Tooltip("音量大小（0.0 = 静音，1.0 = 最大音量）")]
        [Range(0f, 1f)]
        public float volume = 1f;

        /// <summary>
        /// 执行播放音效的效果
        /// </summary>
        /// <param name="item">被拾取的物品（未使用）</param>
        /// <param name="picker">拾取者，音效将在其位置播放</param>
        /// <param name="handler">拾取处理器（未使用）</param>
        public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
        {
            // 检查是否配置了音频片段
            if (pickupSound != null)
            {
                // 在拾取者的位置播放音效
                // PlayClipAtPoint会创建临时GameObject，播放完毕后自动销毁
                AudioSource.PlayClipAtPoint(pickupSound, picker.transform.position, volume);
            }
        }
    }
}
