using UnityEngine;
using Core.Items;

namespace ItemSystem.Conditions
{
    /// <summary>
    /// 拾取条件基类（ScriptableObject）
    /// 可扩展：创建子类实现不同的条件检查逻辑
    /// </summary>
    public abstract class ScriptablePickupCondition : ScriptableObject
    {
        public abstract bool Check(Item item, GameObject picker, ItemPickupHandler handler);
        public abstract string GetFailMessage();
    }
}
