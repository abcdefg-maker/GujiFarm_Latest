using Core.Items;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for inventory management systems
    /// </summary>
    public interface IInventory
    {
        /// <summary>
        /// Maximum number of slots in the inventory
        /// </summary>
        int MaxSlots { get; }

        /// <summary>
        /// Currently selected slot index
        /// </summary>
        int SelectedIndex { get; }

        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>True if the item was successfully added</returns>
        bool AddItem(Item item);

        /// <summary>
        /// Remove an item from a specific slot
        /// </summary>
        /// <param name="index">The slot index</param>
        /// <param name="amount">Amount to remove</param>
        /// <returns>The removed item, or null if failed</returns>
        Item RemoveItem(int index, int amount = 1);

        /// <summary>
        /// Get the currently selected item
        /// </summary>
        /// <returns>The selected item, or null if empty</returns>
        Item GetSelectedItem();

        /// <summary>
        /// Select a specific slot
        /// </summary>
        /// <param name="index">The slot index to select</param>
        void SelectSlot(int index);

        /// <summary>
        /// Check if the inventory is full
        /// </summary>
        /// <returns>True if no empty slots available</returns>
        bool IsFull();
    }
}
