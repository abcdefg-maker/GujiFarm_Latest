using UnityEngine;
using Core.Items;

namespace ItemSystem.Conditions
{
    /// <summary>
    /// 条件：总是允许拾取
    /// </summary>
    [CreateAssetMenu(fileName = "AlwaysAllow", menuName = "Pickup/Conditions/Always Allow")]
    public class AlwaysAllowCondition : ScriptablePickupCondition
    {
        public override bool Check(Item item, GameObject picker, ItemPickupHandler handler)
        {
            return true;
        }

        public override string GetFailMessage()
        {
            return "";
        }
    }
}
