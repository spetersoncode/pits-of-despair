using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Data;

namespace PitsOfDespair.Scripts.Components;

/// <summary>
/// Component that manages equipped items for an entity.
/// Equipped items remain in InventoryComponent but are tracked separately by slot.
/// Requires parent entity to have an InventoryComponent.
/// </summary>
public partial class EquipComponent : Node
{
    /// <summary>
    /// Maps equipment slots to inventory keys (a-z).
    /// If a slot is not in the dictionary, it's empty.
    /// </summary>
    private readonly Dictionary<EquipmentSlot, char> _equippedSlots = new();

    [Signal]
    public delegate void EquipmentChangedEventHandler(EquipmentSlot slot);

    private StatsComponent? _stats;

    public override void _Ready()
    {
        _stats = GetParent().GetNodeOrNull<StatsComponent>("StatsComponent");
    }

    /// <summary>
    /// Equips an item from inventory into the specified slot.
    /// If the slot is already occupied, the old item is automatically unequipped.
    /// Updates attack component if this is a weapon.
    /// </summary>
    /// <param name="inventoryKey">The inventory key (a-z) of the item to equip</param>
    /// <param name="slot">The equipment slot to equip into</param>
    /// <returns>True if successfully equipped, false otherwise</returns>
    public bool Equip(char inventoryKey, EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.None)
        {
            GD.PrintErr("EquipComponent: Cannot equip to None slot");
            return false;
        }

        // If slot is occupied, unequip the old item first
        if (_equippedSlots.ContainsKey(slot))
        {
            Unequip(slot);
        }

        // Mark item as equipped in this slot
        _equippedSlots[slot] = inventoryKey;

        // Apply item bonuses (armor, stats, etc.)
        ApplyItemBonuses(inventoryKey, slot);

        // Update attacks if this is a weapon
        UpdateAttacks();

        EmitSignal(SignalName.EquipmentChanged, (int)slot);
        return true;
    }

    /// <summary>
    /// Unequips an item from the specified slot.
    /// Updates attack component if this was a weapon.
    /// </summary>
    /// <param name="slot">The equipment slot to unequip</param>
    /// <returns>True if an item was unequipped, false if slot was already empty</returns>
    public bool Unequip(EquipmentSlot slot)
    {
        if (!_equippedSlots.ContainsKey(slot))
        {
            return false; // Slot already empty
        }

        // Remove item bonuses before unequipping
        RemoveItemBonuses(slot);

        _equippedSlots.Remove(slot);

        // Update attacks if this was a weapon
        UpdateAttacks();

        EmitSignal(SignalName.EquipmentChanged, (int)slot);
        return true;
    }

    /// <summary>
    /// Gets the inventory key of the item equipped in the specified slot.
    /// Returns null if the slot is empty.
    /// </summary>
    public char? GetEquippedKey(EquipmentSlot slot)
    {
        if (_equippedSlots.TryGetValue(slot, out char key))
        {
            return key;
        }
        return null;
    }

    /// <summary>
    /// Checks if a specific inventory item is currently equipped.
    /// </summary>
    /// <param name="inventoryKey">The inventory key to check</param>
    /// <returns>True if the item is equipped in any slot</returns>
    public bool IsEquipped(char inventoryKey)
    {
        return _equippedSlots.ContainsValue(inventoryKey);
    }

    /// <summary>
    /// Gets the slot that a specific inventory item is equipped in.
    /// Returns EquipmentSlot.None if the item is not equipped.
    /// </summary>
    public EquipmentSlot GetSlotForItem(char inventoryKey)
    {
        foreach (var kvp in _equippedSlots)
        {
            if (kvp.Value == inventoryKey)
            {
                return kvp.Key;
            }
        }
        return EquipmentSlot.None;
    }

    /// <summary>
    /// Updates equipped item keys after inventory key rebinding.
    /// Called when inventory keys are swapped or reassigned.
    /// </summary>
    /// <param name="oldKey">The old inventory key</param>
    /// <param name="newKey">The new inventory key</param>
    public void UpdateEquippedKey(char oldKey, char newKey)
    {
        // Find all slots that have the old key equipped and update them
        foreach (var slot in _equippedSlots.Keys.ToList())
        {
            if (_equippedSlots[slot] == oldKey)
            {
                _equippedSlots[slot] = newKey;
            }
        }
    }


    /// <summary>
    /// Updates the parent entity's AttackComponent based on equipped weapons.
    /// If weapons are equipped, uses their attacks; otherwise falls back to natural attacks.
    /// </summary>
    private void UpdateAttacks()
    {
        var attackComponent = GetParent().GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            return; // Entity doesn't have attacks
        }

        var attacks = new Godot.Collections.Array<AttackData>();

        // Check for equipped melee weapon
        if (_equippedSlots.TryGetValue(EquipmentSlot.MeleeWeapon, out char meleeKey))
        {
            var meleeAttack = GetWeaponAttack(meleeKey);
            if (meleeAttack != null)
            {
                attacks.Add(meleeAttack);
            }
        }

        // Check for equipped ranged weapon
        if (_equippedSlots.TryGetValue(EquipmentSlot.RangedWeapon, out char rangedKey))
        {
            var rangedAttack = GetWeaponAttack(rangedKey);
            if (rangedAttack != null)
            {
                attacks.Add(rangedAttack);
            }
        }

        // If no weapons equipped, fall back to natural attacks
        if (attacks.Count == 0)
        {
            attackComponent.Attacks = attackComponent.NaturalAttacks;
        }
        else
        {
            attackComponent.Attacks = attacks;
        }
    }

    /// <summary>
    /// Gets the attack data for a weapon item.
    /// Retrieves weapon from parent entity's InventoryComponent.
    /// The attack name is already set to the weapon name during item loading.
    /// </summary>
    private AttackData? GetWeaponAttack(char inventoryKey)
    {
        var inventory = GetParent()?.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
            return null;

        var slot = inventory.GetSlot(inventoryKey);
        if (slot?.Item?.Template?.Attack != null)
        {
            return slot.Item.Template.Attack;
        }

        return null;
    }

    /// <summary>
    /// Applies item bonuses to StatsComponent when equipping.
    /// Includes armor value, evasion penalty, and stat bonuses.
    /// </summary>
    private void ApplyItemBonuses(char inventoryKey, EquipmentSlot slot)
    {
        if (_stats == null)
        {
            return; // Entity doesn't have stats
        }

        var itemData = GetItemData(inventoryKey);
        if (itemData == null)
        {
            return;
        }

        // Generate source name for tracking (e.g., "equipped_armor", "equipped_ring1")
        string source = $"equipped_{slot.ToString().ToLower()}";

        // Apply armor value if any
        if (itemData.ArmorValue != 0)
        {
            _stats.AddArmorSource(source, itemData.ArmorValue);
        }

        // Apply evasion penalty if any
        if (itemData.EvasionPenalty != 0)
        {
            _stats.AddEvasionPenaltySource(source, itemData.EvasionPenalty);
        }

        // Apply stat bonuses if any
        if (itemData.StrengthBonus != 0)
        {
            _stats.AddStrengthModifier(source, itemData.StrengthBonus);
        }

        if (itemData.AgilityBonus != 0)
        {
            _stats.AddAgilityModifier(source, itemData.AgilityBonus);
        }

        if (itemData.EnduranceBonus != 0)
        {
            _stats.AddEnduranceModifier(source, itemData.EnduranceBonus);
        }

        if (itemData.WillBonus != 0)
        {
            _stats.AddWillModifier(source, itemData.WillBonus);
        }
    }

    /// <summary>
    /// Removes item bonuses from StatsComponent when unequipping.
    /// </summary>
    private void RemoveItemBonuses(EquipmentSlot slot)
    {
        if (_stats == null)
        {
            return; // Entity doesn't have stats
        }

        // Generate source name (must match what was used in ApplyItemBonuses)
        string source = $"equipped_{slot.ToString().ToLower()}";

        // Remove all possible modifier sources
        // (It's safe to call remove even if the source doesn't exist)
        _stats.RemoveArmorSource(source);
        _stats.RemoveEvasionPenaltySource(source);
        _stats.RemoveStrengthModifier(source);
        _stats.RemoveAgilityModifier(source);
        _stats.RemoveEnduranceModifier(source);
        _stats.RemoveWillModifier(source);
    }

    /// <summary>
    /// Gets the ItemData for an equipped item by inventory key.
    /// Retrieves item from parent entity's InventoryComponent.
    /// </summary>
    private ItemData? GetItemData(char inventoryKey)
    {
        var inventory = GetParent()?.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
            return null;

        var slot = inventory.GetSlot(inventoryKey);
        return slot?.Item?.Template;
    }
}
