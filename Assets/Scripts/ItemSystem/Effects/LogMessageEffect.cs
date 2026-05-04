using UnityEngine;
using Core.Items;

namespace ItemSystem.Effects
{
    /// <summary>
    /// 效果：输出日志消息（主要用于调试）
    ///
    /// 功能：
    /// - 在控制台输出自定义的日志消息
    /// - 支持动态占位符替换（{itemName}、{pickerName}）
    /// - 用于测试拾取逻辑是否正确触发
    ///
    /// 配置参数：
    /// - message: 要输出的日志内容（支持多行文本）
    ///
    /// 占位符说明：
    /// - {itemName}: 会被替换为物品的名称（item.itemName）
    /// - {pickerName}: 会被替换为拾取者的GameObject名称（picker.name）
    ///
    /// 使用场景：
    /// - 开发阶段调试拾取系统
    /// - 测试条件和效果的执行顺序
    /// - 验证拾取逻辑是否按预期工作
    ///
    /// 注意：
    /// - 正式发布前建议移除此效果，避免过多日志输出
    /// - 可以扩展支持更多占位符（如{itemID}、{time}等）
    ///
    /// 创建方式：
    /// Unity编辑器 > 右键 > Create > Pickup/Effects/Log Message
    ///
    /// 示例配置：
    /// message = "玩家 {pickerName} 拾取了 {itemName}"
    /// 输出 = "玩家 Player 拾取了 胡萝卜种子"
    /// </summary>
    [CreateAssetMenu(fileName = "LogMessage", menuName = "Pickup/Effects/Log Message")]
    public class LogMessageEffect : ScriptablePickupEffect
    {
        [Tooltip("要输出的日志消息，支持占位符：{itemName}、{pickerName}")]
        [TextArea]
        public string message = "Item picked up!";

        /// <summary>
        /// 执行输出日志的效果
        /// </summary>
        /// <param name="item">被拾取的物品</param>
        /// <param name="picker">拾取者</param>
        /// <param name="handler">拾取处理器（未使用）</param>
        public override void Execute(Item item, GameObject picker, ItemPickupHandler handler)
        {
            // 使用Replace方法替换占位符
            string formatted = message
                .Replace("{itemName}", item.itemName)      // 替换物品名称
                .Replace("{pickerName}", picker.name);     // 替换拾取者名称

            // 输出格式化后的日志
            Debug.Log(formatted);
        }
    }
}
