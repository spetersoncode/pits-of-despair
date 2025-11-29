using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Components;
using System;

namespace PitsOfDespair.UI;

/// <summary>
/// Configuration flags for formatting item display lines.
/// </summary>
[Flags]
public enum ItemDisplayOptions
{
    None = 0,
    ShowCount = 1 << 0,
    ShowCharges = 1 << 1,
    ShowSlot = 1 << 2,
    ShowEquipped = 1 << 3,
    IsDisabled = 1 << 4,
    ShowChargeBrackets = 1 << 5
}

/// <summary>
/// Utility class for consistent item formatting across all inventory modals.
/// </summary>
public static class ItemFormatter
{
    /// <summary>
    /// Formats an equipment slot name for display.
    /// </summary>
    public static string FormatSlotName(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MeleeWeapon => "Melee",
            EquipmentSlot.RangedWeapon => "Ranged",
            EquipmentSlot.Ammo => "Ammo",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Ring1 => "Ring1",
            EquipmentSlot.Ring2 => "Ring2",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Formats a complete item line for display in inventory modals.
    /// </summary>
    /// <param name="slot">The inventory slot containing the item.</param>
    /// <param name="options">Display options flags.</param>
    /// <param name="equipComponent">Optional equip component to check equipped status.</param>
    /// <returns>Formatted BBCode line for the item.</returns>
    public static string FormatItemLine(InventorySlot slot, ItemDisplayOptions options, EquipComponent equipComponent = null)
    {
        var itemTemplate = slot.Item.Template;
        bool isDisabled = options.HasFlag(ItemDisplayOptions.IsDisabled);

        // Determine colors
        string keyColor = isDisabled ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Disabled);
        string itemColor = isDisabled ? Palette.ToHex(Palette.Disabled) : itemTemplate.Color;

        // Build components
        string key = $"[color={keyColor}]{slot.Key})[/color]";
        string glyph = $"[color={itemColor}]{itemTemplate.GetGlyph()}[/color]";

        // Name with optional count and brands
        string displayName = slot.Item.GetBrandedDisplayName();
        if (options.HasFlag(ItemDisplayOptions.ShowCount) && slot.Item.Quantity > 1)
        {
            // For stackable items with count, append quantity
            displayName = $"{displayName} (x{slot.Item.Quantity})";
        }
        string name = $"[color={itemColor}]{displayName}[/color]";

        // Charges display
        string charges = "";
        int maxCharges = itemTemplate.GetMaxCharges();
        if (maxCharges > 0)
        {
            if (options.HasFlag(ItemDisplayOptions.ShowCharges))
            {
                charges = $" [color={itemColor}][{slot.Item.CurrentCharges}/{maxCharges}][/color]";
            }
            else if (options.HasFlag(ItemDisplayOptions.ShowChargeBrackets))
            {
                string bracket = GetChargeBracket(slot.Item.CurrentCharges, maxCharges);
                charges = $" [color={itemColor}][{bracket}][/color]";
            }
        }

        // Slot indicator for equippable items
        string slotIndicator = "";
        if (options.HasFlag(ItemDisplayOptions.ShowSlot) && itemTemplate.GetIsEquippable())
        {
            var equipSlot = itemTemplate.GetEquipmentSlot();
            slotIndicator = $" [color={Palette.ToHex(Palette.AshGray)}][{FormatSlotName(equipSlot)}][/color]";
        }

        // Equipped indicator
        string equippedIndicator = "";
        if (options.HasFlag(ItemDisplayOptions.ShowEquipped) && equipComponent != null)
        {
            bool isEquipped = equipComponent.IsEquipped(slot.Key);
            if (isEquipped)
            {
                equippedIndicator = $" [color={Palette.ToHex(Palette.Success)}]{{EQUIPPED}}[/color]";
            }
        }

        return $"{key} {glyph} {name}{charges}{slotIndicator}{equippedIndicator}";
    }

    /// <summary>
    /// Gets a descriptive bracket for charge level (used by Attunement skill).
    /// </summary>
    /// <param name="current">Current charges.</param>
    /// <param name="max">Maximum charges.</param>
    /// <returns>Charge level bracket: Full, Half, Low, or Almost Empty.</returns>
    public static string GetChargeBracket(int current, int max)
    {
        if (max <= 0) return "Empty";

        float percent = (float)current / max * 100f;
        return percent switch
        {
            >= 75f => "Full",
            >= 50f => "Half",
            >= 25f => "Low",
            _ => "Almost Empty"
        };
    }
}
