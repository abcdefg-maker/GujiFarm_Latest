using UnityEngine;
using Core.Items;

namespace ItemSystem.Conditions
{
    /// <summary>
    /// 条件：必须空手
    /// </summary>
    [CreateAssetMenu(fileName = "RequireEmptyHand", menuName = "Pickup/Conditions/Require Empty Hand")]
    public class RequireEmptyHandCondition : ScriptablePickupCondition
    {
        public override bool Check(Item item, GameObject picker, ItemPickupHandler handler)
        {
            return handler.HeldItem == null;
        }

        public override string GetFailMessage()
        {
            return "You must empty your hands first!";
        }
    }
}
