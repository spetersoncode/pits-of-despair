using PitsOfDespair.Data;

namespace PitsOfDespair.Data;

/// <summary>
/// Represents a single slot in an entity's inventory.
/// Each slot is assigned a letter key (a-z or A-Z) and can hold multiple items of the same type.
/// For consumables, Count can be > 1. For charged items, Count is always 1.
/// </summary>
public class InventorySlot
{
    /// <summary>
    /// The key assigned to this slot (a-z or A-Z).
    /// </summary>
    public char Key { get; set; }

    /// <summary>
    /// The item instance in this slot, which wraps the template data and per-instance state.
    /// </summary>
    public ItemInstance Item { get; set; }

    /// <summary>
    /// The number of items stacked in this slot.
    /// Only > 1 for consumable items. Charged items always have Count = 1.
    /// </summary>
    public int Count { get; set; }

    public InventorySlot(char key, ItemInstance item, int count = 1)
    {
        Key = key;
        Item = item;
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
