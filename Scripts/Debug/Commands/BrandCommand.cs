using System;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Brands;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to apply brands to equipped weapons.
/// Usage: brand [type] [amount?] [slot?]
/// Examples:
///   brand flaming       - Apply flaming brand to melee weapon
///   brand damage 3      - Apply +3 damage to melee weapon
///   brand vampiric 25 ranged - Apply vampiric to ranged weapon
/// </summary>
public class BrandCommand : DebugCommand
{
    private static readonly string[] ValidBrandTypes = { "damage", "accuracy", "flaming", "freezing", "electrified", "venomous", "vampiric" };
    private static readonly string[] ValidSlots = { "melee", "ranged" };

    public override string Name => "brand";
    public override string Description => "Apply a brand to equipped weapon";
    public override string Usage => "brand [type] [amount?] [slot?]";

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        return argIndex switch
        {
            0 => FilterSuggestions(ValidBrandTypes, currentValue),
            1 => new[] { "1", "2", "3", "5", "25" }, // Amount suggestions
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
                $"Usage: {Usage}\nTypes: {string.Join(", ", ValidBrandTypes)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        string brandType = args[0].ToLower();
        int amount = args.Length > 1 && int.TryParse(args[1], out int a) ? a : 1;
        string slotType = args.Length > 2 ? args[2].ToLower() : "melee";

        // Validate brand type
        if (!BrandFactory.IsValidType(brandType))
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown brand type: {brandType}\nValid types: {string.Join(", ", ValidBrandTypes)}",
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

        // Get equipped weapon
        var slot = slotType == "ranged" ? EquipmentSlot.RangedWeapon : EquipmentSlot.MeleeWeapon;
        var inventoryKey = equipComponent.GetEquippedKey(slot);

        if (inventoryKey == null)
        {
            return DebugCommandResult.CreateFailure(
                $"No {slotType} weapon equipped!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var inventorySlot = inventoryComponent.GetSlot(inventoryKey.Value);
        if (inventorySlot?.Item == null)
        {
            return DebugCommandResult.CreateFailure(
                "Could not find equipped weapon in inventory!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Create and apply the brand
        var brand = BrandFactory.Create(brandType, amount, "permanent");
        if (brand == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Failed to create brand: {brandType}",
                Palette.ToHex(Palette.Danger)
            );
        }

        var message = inventorySlot.Item.AddBrand(brand);
        string weaponName = inventorySlot.Item.GetBrandedDisplayName();

        return DebugCommandResult.CreateSuccess(
            $"Applied [b]{brand.Name}[/b] to {weaponName}!",
            Palette.ToHex(Palette.Success)
        );
    }
}
