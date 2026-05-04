using UnityEngine;

namespace ToolSystem.Actions
{
    /// <summary>
    /// 工具行为基类（ScriptableObject）
    /// </summary>
    public abstract class ScriptableToolAction : ScriptableObject
    {
        [Header("行为信息")]
        [Tooltip("行为名称")]
        [SerializeField] protected string actionName = "Tool Action";
        public string ActionName => actionName;

        /// <summary>
        /// 执行工具行为
        /// </summary>
        public virtual void Execute(ToolItem tool, GameObject user, IToolTarget target)
        {
            if (target != null)
            {
                target.ReceiveToolAction(tool, user);
            }

            OnExecute(tool, user, target);
        }

        /// <summary>
        /// 子类实现具体的行为逻辑
        /// </summary>
        protected abstract void OnExecute(ToolItem tool, GameObject user, IToolTarget target);
    }
}
