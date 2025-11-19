using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
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
    IsDisabled = 1 << 4
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

        // Name with optional count
        string displayName = itemTemplate.Name;
        if (options.HasFlag(ItemDisplayOptions.ShowCount))
        {
            displayName = itemTemplate.GetDisplayName(slot.Count);
        }
        string name = $"[color={itemColor}]{displayName}[/color]";

        // Charges (currently hidden as per user requirement)
        string charges = "";
        if (options.HasFlag(ItemDisplayOptions.ShowCharges) && itemTemplate.GetMaxCharges() > 0)
        {
            charges = $" [color={itemColor}][{slot.Item.CurrentCharges}/{itemTemplate.GetMaxCharges()}][/color]";
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
}
