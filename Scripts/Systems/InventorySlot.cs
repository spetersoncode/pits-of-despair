using PitsOfDespair.Data;

namespace PitsOfDespair.Systems;

/// <summary>
/// Represents a single slot in the player's inventory.
/// Each slot is assigned a letter key (a-z) and can hold multiple items of the same type.
/// </summary>
public class InventorySlot
{
    /// <summary>
    /// The key assigned to this slot (a-z).
    /// </summary>
    public char Key { get; set; }

    /// <summary>
    /// The item data for items in this slot.
    /// </summary>
    public ItemData ItemData { get; set; }

    /// <summary>
    /// The number of items stacked in this slot.
    /// </summary>
    public int Count { get; set; }

    public InventorySlot(char key, ItemData itemData, int count = 1)
    {
        Key = key;
        ItemData = itemData;
        Count = count;
    }

    /// <summary>
    /// Adds items to this slot.
    /// </summary>
    public void Add(int amount = 1)
    {
        Count += amount;
    }

    /// <summary>
    /// Removes items from this slot.
    /// Returns true if slot is now empty.
    /// </summary>
    public bool Remove(int amount = 1)
    {
        Count -= amount;
        return Count <= 0;
    }
}
