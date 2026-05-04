namespace ToolSystem
{
    /// <summary>
    /// 可被工具作用的目标接口
    /// 实现此接口的对象可以被工具交互
    /// </summary>
    public interface IToolTarget
    {
        /// <summary>
        /// 检查指定工具是否可以与此目标交互
        /// </summary>
        /// <param name="tool">要使用的工具</param>
        /// <returns>是否可以交互</returns>
        bool CanInteract(ToolItem tool);

        /// <summary>
        /// 接收工具作用
        /// </summary>
        /// <param name="tool">使用的工具</param>
        /// <param name="user">使用者</param>
        void ReceiveToolAction(ToolItem tool, UnityEngine.GameObject user);

        /// <summary>
        /// 获取该目标接受的工具类型
        /// </summary>
        /// <returns>接受的工具类型数组</returns>
        ToolType[] GetAcceptedToolTypes();
    }
}
