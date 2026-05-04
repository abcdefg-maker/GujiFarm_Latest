using UnityEngine;

namespace ToolSystem.Actions
{
    /// <summary>
    /// 收割行为
    /// </summary>
    [CreateAssetMenu(fileName = "HarvestingAction", menuName = "Tools/Actions/Harvesting Action")]
    public class HarvestingAction : ScriptableToolAction
    {
        protected override void OnExecute(ToolItem tool, GameObject user, IToolTarget target)
        {
            // TODO: 实现收割逻辑
            Debug.Log($"[HarvestingAction] {user.name} 使用 {tool.itemName} 收割");
        }
    }
}
