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
/// Usage: property [type] [amount?] [slot?]
/// Examples:
///   property flaming       - Apply flaming property to melee weapon
///   property damage 3      - Apply +3 damage to melee weapon
///   property vampiric 25 ranged - Apply vampiric to ranged weapon
///   property thorns 2 armor - Apply thorns to equipped armor
///   property resistance_fire 50 armor - Apply 50% fire resistance to armor
/// </summary>
public class PropertyCommand : DebugCommand
{
    private static readonly string[] WeaponPropertyTypes = { "damage", "accuracy", "flaming", "freezing", "electrified", "venomous", "vampiric" };
    private static readonly string[] ArmorPropertyTypes = { "thorns", "resistance_fire", "resistance_cold", "resistance_lightning", "resistance_poison", "resistance_necrotic", "resistance_acid", "resistance_slashing", "resistance_piercing", "resistance_bludgeoning", "armor", "evasion", "regen", "max_health" };
    private static readonly string[] AllPropertyTypes = WeaponPropertyTypes.Concat(ArmorPropertyTypes).ToArray();
    private static readonly string[] ValidSlots = { "melee", "ranged", "armor", "ring1", "ring2" };

    public override string Name => "property";
    public override string Description => "Apply a property to equipped item";
    public override string Usage => "property [type] [amount?] [slot?]";

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        return argIndex switch
        {
            0 => FilterSuggestions(AllPropertyTypes, currentValue),
            1 => new[] { "1", "2", "3", "5", "25", "50" }, // Amount suggestions
            2 => FilterSuggestions(ValidSlots, currentValue),
            _ => null
        };
    }

    private static IReadOnlyList<string> FilterSuggestions(string[] options, string currentValue)
    {
        if (string.IsNullOrEmpty(currentValue))
            return options;

        return options.Where(o => o.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                $"Usage: {Usage}\nWeapon: {string.Join(", ", WeaponPropertyTypes)}\nArmor: {string.Join(", ", ArmorPropertyTypes)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        string propertyType = args[0].ToLower();
        int amount = args.Length > 1 && int.TryParse(args[1], out int a) ? a : 1;
        string slotType = args.Length > 2 ? args[2].ToLower() : "melee";

        // Validate property type
        if (!ItemPropertyFactory.IsValidType(propertyType))
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown property type: {propertyType}",
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
