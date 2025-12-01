using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Data;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.ItemProperties;

namespace PitsOfDespair.Components;

/// <summary>
/// Component that manages equipped items for an entity.
/// Equipped items remain in InventoryComponent but are tracked separately by slot.
/// Requires parent entity to have an InventoryComponent.
/// </summary>
public partial class EquipComponent : Node
{
    /// <summary>
    /// Maps equipment slots to inventory keys (a-z or A-Z).
    /// If a slot is not in the dictionary, it's empty.
    /// </summary>
    private readonly Dictionary<EquipmentSlot, char> _equippedSlots = new();

    [Signal]
    public delegate void EquipmentChangedEventHandler(EquipmentSlot slot);

    private StatsComponent? _stats;
    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent() as BaseEntity;
        _stats = GetParent().GetNodeOrNull<StatsComponent>("StatsComponent");
    }

    /// <summary>
    /// Equips an item from inventory into the specified slot.
    /// If the slot is already occupied, the old item is automatically unequipped.
    /// Updates attack component if this is a weapon.
    /// </summary>
    /// <param name="inventoryKey">The inventory key (a-z or A-Z) of the item to equip</param>
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
    /// Gets an available ring slot, or null if both are occupied.
    /// Prefers Ring1, falls back to Ring2.
    /// </summary>
    public EquipmentSlot? GetAvailableRingSlot()
    {
        if (!_equippedSlots.ContainsKey(EquipmentSlot.Ring1))
            return EquipmentSlot.Ring1;
        if (!_equippedSlots.ContainsKey(EquipmentSlot.Ring2))
            return EquipmentSlot.Ring2;
        return null;
    }

    /// <summary>
    /// Gets the appropriate equipment slot for an item, handling rings dynamically.
    /// For rings, returns an available ring slot (Ring1 or Ring2).
    /// For other items, returns the item's defined slot.
    /// </summary>
    /// <param name="itemData">The item to get the slot for</param>
    /// <returns>The equipment slot, or None if no slot available</returns>
    public EquipmentSlot GetSlotForItem(ItemData itemData)
    {
        if (itemData.IsRing)
        {
            return GetAvailableRingSlot() ?? EquipmentSlot.Ring1; // Fall back to Ring1 (will swap)
        }
        return itemData.GetEquipmentSlot();
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

        // Remove item bonuses before unequipping (need the key before we remove it)
        char inventoryKey = _equippedSlots[slot];
        RemoveItemBonuses(slot, inventoryKey);

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
    /// Gets the slot that a specific inventory item is currently equipped in.
    /// Returns EquipmentSlot.None if the item is not equipped.
    /// </summary>
    public EquipmentSlot GetEquippedSlotForItem(char inventoryKey)
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
    /// Applies item stat modifiers as conditions when equipping.
    /// Reads stat properties from ItemData and ItemInstance properties, creates WhileEquipped conditions.
    /// </summary>
    private void ApplyItemBonuses(char inventoryKey, EquipmentSlot slot)
    {
        if (_entity == null)
        {
            return;
        }

        var itemData = GetItemData(inventoryKey);
        if (itemData == null)
        {
            return;
        }

        // Generate source prefix for tracking (e.g., "equipped_armor_a", "equipped_ring1_b")
        string sourcePrefix = $"equipped_{slot.ToString().ToLower()}_{inventoryKey}";

        // Apply base stat modifiers from ItemData
        ApplyStatCondition(itemData.Armor, "armor_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Evasion, "evasion_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Strength, "strength_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Agility, "agility_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Endurance, "endurance_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Will, "will_modifier", sourcePrefix);
        ApplyStatCondition(itemData.MaxHealth, "max_health_modifier", sourcePrefix);
        ApplyStatCondition(itemData.MaxWillpower, "max_willpower_modifier", sourcePrefix);
        ApplyStatCondition(itemData.Regen, "regen_modifier", sourcePrefix);

        // Apply enhancement bonuses from item properties (IStatBonusProperty)
        var itemInstance = GetItemInstance(inventoryKey);
        if (itemInstance != null)
        {
            ApplyPropertyStatBonuses(itemInstance, $"{sourcePrefix}_prop");
        }
    }

    /// <summary>
    /// Applies stat bonuses from item properties (IStatBonusProperty).
    /// </summary>
    private void ApplyPropertyStatBonuses(ItemInstance item, string sourcePrefix)
    {
        if (_entity == null) return;

        foreach (var property in item.GetProperties())
        {
            if (property is IStatBonusProperty statProperty)
            {
                string conditionType = statProperty.BonusType switch
                {
                    StatBonusType.Armor => "armor_modifier",
                    StatBonusType.Evasion => "evasion_modifier",
                    StatBonusType.Regen => "regen_modifier",
                    StatBonusType.MaxHealth => "max_health_modifier",
                    _ => null
                };

                if (conditionType != null)
                {
                    ApplyStatCondition(statProperty.GetBonus(), conditionType, sourcePrefix);
                }
            }
        }
    }

    /// <summary>
    /// Creates and applies a stat condition if the value is non-null.
    /// </summary>
    private void ApplyStatCondition(int? value, string conditionType, string sourcePrefix)
    {
        if (value == null || _entity == null)
        {
            return;
        }

        string sourceId = sourcePrefix;

        var condition = ConditionFactory.Create(
            conditionType,
            value.Value,
            "1", // Duration doesn't matter for WhileEquipped
            ConditionDuration.WhileEquipped,
            sourceId
        );

        if (condition != null)
        {
            _entity.AddCondition(condition);
        }
    }

    /// <summary>
    /// Removes item conditions when unequipping.
    /// Removes all conditions with the slot's source prefix (both base and property bonuses).
    /// </summary>
    private void RemoveItemBonuses(EquipmentSlot slot, char inventoryKey)
    {
        if (_entity == null)
        {
            return;
        }

        // Generate source prefix (must match what was used in ApplyItemBonuses)
        string sourcePrefix = $"equipped_{slot.ToString().ToLower()}_{inventoryKey}";

        // Remove all conditions from this equipment slot (base stats)
        _entity.RemoveConditionsBySource(sourcePrefix);

        // Remove property-based stat bonuses
        _entity.RemoveConditionsBySource($"{sourcePrefix}_prop");
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

    /// <summary>
    /// Gets the ItemInstance for an equipped item by inventory key.
    /// </summary>
    public ItemInstance? GetItemInstance(char inventoryKey)
    {
        var inventory = GetParent()?.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
            return null;

        var slot = inventory.GetSlot(inventoryKey);
        return slot?.Item;
    }

    /// <summary>
    /// Gets the ItemInstance equipped in a specific slot.
    /// Returns null if slot is empty.
    /// </summary>
    public ItemInstance? GetEquippedItem(EquipmentSlot slot)
    {
        if (!_equippedSlots.TryGetValue(slot, out char key))
            return null;
        return GetItemInstance(key);
    }

    /// <summary>
    /// Gets all properties of a specific interface type from equipped defensive items (Armor, Ring1, Ring2).
    /// </summary>
    /// <typeparam name="T">The property interface type to retrieve.</typeparam>
    /// <returns>Enumerable of all matching properties from equipped items.</returns>
    public IEnumerable<T> GetDefensiveProperties<T>() where T : class
    {
        EquipmentSlot[] defensiveSlots = { EquipmentSlot.Armor, EquipmentSlot.Ring1, EquipmentSlot.Ring2 };

        foreach (var slot in defensiveSlots)
        {
            var item = GetEquippedItem(slot);
            if (item == null) continue;

            foreach (var property in item.GetProperties())
            {
                if (property is T typedProperty)
                {
                    yield return typedProperty;
                }
            }
        }
    }
}
