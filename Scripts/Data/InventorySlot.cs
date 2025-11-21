using PitsOfDespair.Data;

namespace PitsOfDespair.Data;

/// <summary>
/// Represents a single slot in an entity's inventory.
/// Each slot is assigned a letter key (a-z or A-Z) and holds a single ItemInstance.
/// For stackable items, the quantity is tracked in ItemInstance.Quantity.
/// </summary>
public class InventorySlot
{
    /// <summary>
    /// The key assigned to this slot (a-z or A-Z).
    /// </summary>
    public char Key { get; set; }

    /// <summary>
    /// The item instance in this slot, which wraps the template data and per-instance state.
    /// For stackable items, Item.Quantity indicates how many are in this stack.
    /// </summary>
    public ItemInstance Item { get; set; }

    public InventorySlot(char key, ItemInstance item)
    {
        Key = key;
        Item = item;
    }
}
