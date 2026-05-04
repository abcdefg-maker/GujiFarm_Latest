using UnityEngine;
using Core.Items;

namespace ItemSystem.Effects
{
    /// <summary>
    /// 效果：禁用物品的碰撞体组件
    ///
    /// 功能：
    /// - 拾取物品后禁用其Collider组件
    /// - 防止已拾取的物品与其他对象产生物理碰撞
    ///
    /// 使用场景：
    /// - 手持物品时避免碰撞体干扰玩家移动
    /// - 防止拾取后的物品被再次检测为可拾取目标
    /// - 避免手持物品与场景中其他碰撞体产生意外交互
    ///
    /// 注意：
    /// - 仅禁用第一个找到的Collider组件
    /// - 如果物品有多个Collider，需要扩展此效果
    /// - 物品被放置时，需要在Item.OnDropped()中重新启用碰撞体
    ///
    /// 创建方式：
    /// Unity编辑器 > 右键 > Create > Pickup/Effects/Disable Collider
    /// </summary>
    [CreateAssetMenu(fileName = "DisableCollider", menuName = "Pickup/Effects/Disable Collider")]
    public class DisableColliderEffect : ScriptablePickupEffect
    {
        /// <summary>
        /// 执行禁用碰撞体的效果
        /// </summary>
        /// <param name="item">被拾取的物品</param>
        /// <param name="picker">拾取者（未使用）</param>
        /// <param name="handler">拾取处理器（未使用）</param>
        public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
        {
            // 获取物品上的Collider组件
            var collider = item.GetComponent<Collider>();

            // 如果存在Collider，则禁用它
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}
