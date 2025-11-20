using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing an entity's inventory system.
/// Handles item storage, stacking, and slot management.
/// </summary>
public partial class InventoryComponent : Node
{
    /// <summary>
    /// Emitted when inventory contents change (item added, removed, or count changed).
    /// </summary>
    [Signal]
    public delegate void InventoryChangedEventHandler();

    /// <summary>
    /// Maximum number of unique items that can be stored in inventory.
    /// Consumables can stack, so actual item count may be higher.
    /// Default is 52 slots (a-z + A-Z).
    /// </summary>
    [Export]
    public int MaxInventorySlots { get; set; } = 52;

    private List<InventorySlot> _inventory = new();

    /// <summary>
    /// Gets the inventory slots (read-only).
    /// </summary>
    public IReadOnlyList<InventorySlot> Inventory => _inventory.AsReadOnly();

    /// <summary>
    /// Gets the number of occupied inventory slots.
    /// </summary>
    public int Count => _inventory.Count;

    /// <summary>
    /// Checks if the inventory is full (all slots occupied).
    /// </summary>
    public bool IsFull => _inventory.Count >= MaxInventorySlots;

    /// <summary>
    /// Gets an inventory slot by its key binding.
    /// </summary>
    /// <param name="key">The key to look up (a-z or A-Z).</param>
    /// <returns>The inventory slot, or null if not found.</returns>
    public InventorySlot? GetSlot(char key)
    {
        return _inventory.FirstOrDefault(slot => slot.Key == key);
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// Handles stacking for consumables and slot management.
    /// </summary>
    /// <param name="itemInstance">The item instance to add.</param>
    /// <param name="message">Output message describing the result.</param>
    /// <param name="excludeEquipped">If true, won't stack with equipped items.</param>
    /// <returns>The inventory key assigned to the item, or null if inventory is full.</returns>
    public char? AddItem(ItemInstance itemInstance, out string message, bool excludeEquipped = false)
    {
        // Check if item is stackable (consumables only, charged items never stack)
        bool canStack = itemInstance.Template.GetIsConsumable();

        // Get equipped items to exclude from stacking if requested
        InventorySlot? existingSlot = null;

        if (canStack)
        {
            if (excludeEquipped)
            {
                var equipComponent = GetParent()?.GetNodeOrNull<EquipComponent>("EquipComponent");
                existingSlot = _inventory.FirstOrDefault(slot =>
                    slot.Item.Template.DataFileId == itemInstance.Template.DataFileId &&
                    (equipComponent == null || !equipComponent.IsEquipped(slot.Key)));
            }
            else
            {
                existingSlot = _inventory.FirstOrDefault(slot =>
                    slot.Item.Template.DataFileId == itemInstance.Template.DataFileId);
            }
        }

        // Check if inventory is full
        if (_inventory.Count >= MaxInventorySlots)
        {
            if (existingSlot == null)
            {
                message = $"Inventory full! ({MaxInventorySlots} unique items)";
                return null;
            }

            // Stack with existing item
            existingSlot.Add(1);
            message = $"Added to existing stack ({existingSlot.Count} total).";
            EmitSignal(SignalName.InventoryChanged);
            return existingSlot.Key;
        }

        // Try to find existing slot for stacking
        if (existingSlot != null)
        {
            // Stack with existing item
            existingSlot.Add(1);
            message = $"Added to existing stack ({existingSlot.Count} total).";
            EmitSignal(SignalName.InventoryChanged);
            return existingSlot.Key;
        }

        // Add new slot with next available key
        char nextKey = GetNextAvailableKey();
        var newSlot = new InventorySlot(nextKey, itemInstance, 1);
        _inventory.Add(newSlot);

        message = $"Added to inventory slot '{nextKey}'.";
        EmitSignal(SignalName.InventoryChanged);
        return nextKey;
    }

    /// <summary>
    /// Removes items from the inventory.
    /// </summary>
    /// <param name="key">The inventory slot key (a-z or A-Z).</param>
    /// <param name="count">The number of items to remove.</param>
    /// <returns>True if items were removed successfully.</returns>
    public bool RemoveItem(char key, int count = 1)
    {
        var slot = GetSlot(key);
        if (slot == null)
        {
            return false;
        }

        // Remove the specified count
        slot.Remove(count);

        // If slot is empty, remove it from inventory
        if (slot.Count <= 0)
        {
            _inventory.Remove(slot);
        }

        // Notify listeners and return success
        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Gets the next available inventory key (a-z, then A-Z).
    /// </summary>
    private char GetNextAvailableKey()
    {
        // First check lowercase a-z
        for (char c = 'a'; c <= 'z'; c++)
        {
            if (!_inventory.Any(slot => slot.Key == c))
            {
                return c;
            }
        }

        // Then check uppercase A-Z
        for (char c = 'A'; c <= 'Z'; c++)
        {
            if (!_inventory.Any(slot => slot.Key == c))
            {
                return c;
            }
        }

        // This shouldn't happen due to MaxInventorySlots check, but just in case
        return 'Z';
    }

    /// <summary>
    /// Rebinds an item from one key to another.
    /// If the target key is occupied, swaps the items.
    /// Also updates EquipComponent to keep equipped items tracking the correct keys.
    /// </summary>
    /// <param name="oldKey">The current key of the item to rebind.</param>
    /// <param name="newKey">The desired new key for the item.</param>
    /// <returns>True if rebinding was successful, false if oldKey doesn't exist or keys are the same.</returns>
    public bool RebindItemKey(char oldKey, char newKey)
    {
        // Validate input
        if (oldKey == newKey)
        {
            return false;
        }

        // Find the source slot
        var sourceSlot = GetSlot(oldKey);
        if (sourceSlot == null)
        {
            return false;
        }

        // Get EquipComponent to update equipped keys
        var equipComponent = GetParent()?.GetNodeOrNull<Scripts.Components.EquipComponent>("EquipComponent");

        // Check if target key is occupied
        var targetSlot = GetSlot(newKey);

        if (targetSlot != null)
        {
            // Update equipped keys BEFORE swapping inventory keys
            // This ensures equipped status follows the items, not the keys
            if (equipComponent != null)
            {
                // Use a temporary key that's not in use to avoid conflicts
                char tempKey = '\0';

                // Move oldKey's equipped status to temp
                equipComponent.UpdateEquippedKey(oldKey, tempKey);
                // Move newKey's equipped status to oldKey
                equipComponent.UpdateEquippedKey(newKey, oldKey);
                // Move temp (originally oldKey) to newKey
                equipComponent.UpdateEquippedKey(tempKey, newKey);
            }

            // Swap the inventory keys
            sourceSlot.Key = newKey;
            targetSlot.Key = oldKey;
        }
        else
        {
            // Simple reassignment
            sourceSlot.Key = newKey;

            // Update equipped key in EquipComponent
            if (equipComponent != null)
            {
                equipComponent.UpdateEquippedKey(oldKey, newKey);
            }
        }

        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Clears all items from the inventory.
    /// </summary>
    public void Clear()
    {
        _inventory.Clear();
        EmitSignal(SignalName.InventoryChanged);
    }
}
