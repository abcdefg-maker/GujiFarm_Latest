using UnityEngine;
using Core.Items;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for all items in the game
    /// </summary>
    public interface IItem
    {
        /// <summary>
        /// Unique identifier for the item
        /// </summary>
        string ItemID { get; }

        /// <summary>
        /// Display name of the item
        /// </summary>
        string ItemName { get; }

        /// <summary>
        /// Type of the item (Material, Tool, Consumable, Seed, Crop)
        /// </summary>
        ItemType ItemType { get; }

        /// <summary>
        /// Icon sprite for UI display
        /// </summary>
        Sprite ItemIcon { get; }

        /// <summary>
        /// Whether this item can be picked up
        /// </summary>
        bool CanBePickedUp { get; }

        /// <summary>
        /// Whether this item is currently being held
        /// </summary>
        bool IsPickedUp { get; }

        /// <summary>
        /// The GameObject currently holding this item
        /// </summary>
        GameObject CurrentHolder { get; }

        /// <summary>
        /// Called when the item is picked up
        /// </summary>
        /// <param name="picker">The GameObject picking up the item</param>
        void OnPickedUp(GameObject picker);

        /// <summary>
        /// Called when the item is dropped
        /// </summary>
        /// <param name="dropPosition">The position to drop the item</param>
        void OnDropped(Vector3 dropPosition);
    }
}
