using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for items that can be equipped (held in hand)
    /// </summary>
    public interface IEquippable
    {
        /// <summary>
        /// Equip this item to the specified hand position
        /// </summary>
        /// <param name="hand">The transform to attach to</param>
        void Equip(Transform hand);

        /// <summary>
        /// Unequip this item
        /// </summary>
        void Unequip();

        /// <summary>
        /// Whether this item is currently equipped
        /// </summary>
        bool IsEquipped { get; }
    }
}
