using System;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.ItemProperties;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to apply properties to equipped items.
/// Usage: property [slot] [propname] [amount?]
/// Examples:
///   property melee flaming           - Apply flaming property to melee weapon
///   property melee weapon_enhancement 2 - Apply +2 weapon enhancement
///   property armor armor_enhancement 2  - Apply +2 armor enhancement
///   property ring1 evasion           - Apply evasion to ring in slot 1
///   property ammo flaming            - Apply flaming to equipped ammo
/// </summary>
public class PropertyCommand : DebugCommand
{
    private static readonly string[] ValidSlots = { "melee", "ranged", "armor", "ring1", "ring2", "ammo" };

    public override string Name => "property";
    public override string Description => "Apply a property to equipped item";
    public override string Usage => "property [slot] [propname] [amount?]";

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        return argIndex switch
        {
            0 => FilterSuggestions(ValidSlots, currentValue),
            1 => GetPropertySuggestionsForSlot(currentValue),
            2 => new[] { "1", "2", "3", "5", "25", "50" }, // Amount suggestions
            _ => null
        };
    }

    private IReadOnlyList<string> GetPropertySuggestionsForSlot(string currentValue)
    {
        // Get all property types and filter
        var allTypes = new List<string>();
        foreach (var itemType in new[] { ItemType.Weapon, ItemType.Armor, ItemType.Ring, ItemType.Ammo })
        {
            allTypes.AddRange(ItemPropertyFactory.GetValidPropertyTypes(itemType));
        }
        return FilterSuggestions(allTypes.Distinct().ToArray(), currentValue);
    }

    private static IReadOnlyList<string> FilterSuggestions(string[] options, string currentValue)
    {
        if (string.IsNullOrEmpty(currentValue))
            return options;

        return options.Where(o => o.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return DebugCommandResult.CreateFailure(
                $"Usage: {Usage}\nSlots: {string.Join(", ", ValidSlots)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        string slotType = args[0].ToLower();
        string propertyType = args[1].ToLower();
        int amount = args.Length > 2 && int.TryParse(args[2], out int a) ? a : 0;

        // Validate slot
        if (!ValidSlots.Contains(slotType))
        {
            return DebugCommandResult.CreateFailure(
                $"Invalid slot: {slotType}\nValid slots: {string.Join(", ", ValidSlots)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Validate property type
        if (!ItemPropertyFactory.IsValidType(propertyType))
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown property type: {propertyType}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Check if property is valid for slot's item type
        var itemType = ItemPropertyFactory.ParseSlotType(slotType);
        var validProperties = ItemPropertyFactory.GetValidPropertyTypes(itemType);
        if (!validProperties.Contains(propertyType))
        {
            return DebugCommandResult.CreateFailure(
                $"Property '{propertyType}' cannot be applied to {slotType}\nValid for {slotType}: {string.Join(", ", validProperties)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Get player components
        var player = context.ActionContext.Player;
        var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
        var inventoryComponent = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");

        if (equipComponent == null || inventoryComponent == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player missing equipment or inventory component!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Map slot type string to EquipmentSlot
        var slot = slotType switch
        {
            "ranged" => EquipmentSlot.RangedWeapon,
            "armor" => EquipmentSlot.Armor,
            "ring1" => EquipmentSlot.Ring1,
            "ring2" => EquipmentSlot.Ring2,
            "ammo" => EquipmentSlot.Ammo,
            _ => EquipmentSlot.MeleeWeapon
        };

        var inventoryKey = equipComponent.GetEquippedKey(slot);

        if (inventoryKey == null)
        {
            return DebugCommandResult.CreateFailure(
                $"No item equipped in {slotType} slot!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var inventorySlot = inventoryComponent.GetSlot(inventoryKey.Value);
        if (inventorySlot?.Item == null)
        {
            return DebugCommandResult.CreateFailure(
                "Could not find equipped item in inventory!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Use default amount from metadata if not specified
        if (amount == 0)
        {
            var metadata = ItemPropertyFactory.GetMetadata(propertyType);
            amount = metadata?.MinAmount ?? 1;
        }

        // Create and apply the property
        var property = ItemPropertyFactory.Create(propertyType, amount, "permanent");
        if (property == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Failed to create property: {propertyType}",
                Palette.ToHex(Palette.Danger)
            );
        }

        var message = inventorySlot.Item.AddProperty(property);
        string itemName = inventorySlot.Item.GetDisplayName();

        return DebugCommandResult.CreateSuccess(
            $"Applied [b]{property.Name}[/b] to {itemName}!",
            Palette.ToHex(Palette.Success)
        );
    }
}
