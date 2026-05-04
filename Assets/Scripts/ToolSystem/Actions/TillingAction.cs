using UnityEngine;

namespace ToolSystem.Actions
{
    /// <summary>
    /// 翻地行为
    /// </summary>
    [CreateAssetMenu(fileName = "TillingAction", menuName = "Tools/Actions/Tilling Action")]
    public class TillingAction : ScriptableToolAction
    {
        protected override void OnExecute(ToolItem tool, GameObject user, IToolTarget target)
        {
            // TODO: 实现翻地逻辑
            Debug.Log($"[TillingAction] {user.name} 使用 {tool.itemName} 翻地");
        }
    }
}
