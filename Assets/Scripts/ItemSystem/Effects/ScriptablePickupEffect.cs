using UnityEngine;
using Core.Items;

namespace ItemSystem.Effects
{
    /// <summary>
    /// 拾取效果基类（ScriptableObject）
    ///
    /// 设计模式：策略模式 - 将拾取后的效果行为封装成可配置的资源
    ///
    /// 用途：
    /// - 定义拾取物品后执行的各种效果（如附加到手部、播放音效、禁用碰撞等）
    /// - 通过继承此类创建不同的效果实现
    /// - 在Unity编辑器中创建ScriptableObject资源并配置到ItemPickupHandler
    ///
    /// 可扩展性：
    /// - 创建子类实现Execute方法即可添加新效果
    /// - 支持链式组合：一次拾取可执行多个效果
    /// - 策划可在编辑器中自由配置，无需修改代码
    ///
    /// 示例用法：
    /// 1. 创建子类：public class MyEffect : ScriptablePickupEffect { ... }
    /// 2. 在Unity中右键创建资源：Create > Pickup/Effects/My Effect
    /// 3. 将资源添加到ItemPickupHandler的pickupEffects列表
    /// </summary>
    public abstract class ScriptablePickupEffect : ScriptableObject
    {
        /// <summary>
        /// 执行拾取效果的抽象方法
        /// </summary>
        /// <param name="item">被拾取的物品对象</param>
        /// <param name="picker">执行拾取的GameObject（通常是Player）</param>
        /// <param name="handler">拾取处理器，包含当前持有物品、手部位置等信息</param>
        public abstract void Execute(Item item, GameObject picker, ItemPickupHandler handler);
    }
}
