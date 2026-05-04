using UnityEngine;
using Core.Items;

namespace ItemSystem.Effects
{
    /// <summary>
    /// 效果：将物品附加到拾取者的手部位置
    ///
    /// 功能：
    /// - 将物品的Transform设置为手部位置的子物体
    /// - 重置物品的本地坐标和旋转，使其紧贴手部
    ///
    /// 使用场景：
    /// - 玩家拾取工具（锄头、镰刀等）
    /// - 玩家拾取可携带物品（箱子、木材等）
    /// - 任何需要"手持"视觉效果的物品
    ///
    /// 创建方式：
    /// Unity编辑器 > 右键 > Create > Pickup/Effects/Attach To Hand
    /// </summary>
    [CreateAssetMenu(fileName = "AttachToHand", menuName = "Pickup/Effects/Attach To Hand")]
    public class AttachToHandEffect : ScriptablePickupEffect
    {
        /// <summary>
        /// 执行附加到手部的效果
        /// </summary>
        /// <param name="item">被拾取的物品</param>
        /// <param name="picker">拾取者（未使用）</param>
        /// <param name="handler">拾取处理器，提供handPosition引用</param>
        public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
        {
            // 设置物品的父对象为手部位置
            item.transform.SetParent(handler.handPosition);

            // 重置本地坐标，使物品位于手部中心
            item.transform.localPosition = Vector3.zero;

            // 重置本地旋转，使物品朝向与手部一致
            item.transform.localRotation = Quaternion.identity;
        }
    }
}
