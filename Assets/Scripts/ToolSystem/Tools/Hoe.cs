using UnityEngine;

namespace ToolSystem.Tools
{
    /// <summary>
    /// 锄头工具 - 用于翻地耕作
    /// </summary>
    public class Hoe : ToolItem
    {
        protected override void Awake()
        {
            base.Awake();
            toolType = ToolType.Hoe;
        }
    }
}
