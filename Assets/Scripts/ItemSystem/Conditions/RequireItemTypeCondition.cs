using UnityEngine;
using Core.Items;

namespace ItemSystem.Conditions
{
    /// <summary>
    /// 条件：需要特定物品类型（后续可扩展）
    /// </summary>
    [CreateAssetMenu(fileName = "RequireItemType", menuName = "Pickup/Conditions/Require Item Type")]
    public class RequireItemTypeCondition : ScriptablePickupCondition
    {
        public ItemType requiredType;

        public override bool Check(Item item, GameObject picker, ItemPickupHandler handler)
        {
            // 后续可以检查背包中是否有指定类型的物品
            // 这里暂时返回true
            return true;
        }

        public override string GetFailMessage()
        {
            return $"You need a {requiredType} to pick this up!";
        }
    }
}
