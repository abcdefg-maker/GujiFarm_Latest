using UnityEngine;

namespace ToolSystem.Tools
{
    /// <summary>
    /// 水壶工具 - 用于浇灌农田
    /// </summary>
    public class WateringCan : ToolItem
    {
        protected override void Awake()
        {
            base.Awake();
            toolType = ToolType.WateringCan;
        }
    }
}
