using UnityEngine;

namespace ToolSystem.Tools
{
    /// <summary>
    /// 镰刀工具 - 用于收割作物
    /// </summary>
    public class Sickle : ToolItem
    {
        protected override void Awake()
        {
            base.Awake();
            toolType = ToolType.Sickle;
        }
    }
}
