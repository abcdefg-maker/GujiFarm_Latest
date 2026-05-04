using UnityEngine;

namespace ToolSystem.Actions
{
    /// <summary>
    /// 浇水行为
    /// </summary>
    [CreateAssetMenu(fileName = "WateringAction", menuName = "Tools/Actions/Watering Action")]
    public class WateringAction : ScriptableToolAction
    {
        protected override void OnExecute(ToolItem tool, GameObject user, IToolTarget target)
        {
            // TODO: 实现浇水逻辑
            Debug.Log($"[WateringAction] {user.name} 使用 {tool.itemName} 浇水");
        }
    }
}
