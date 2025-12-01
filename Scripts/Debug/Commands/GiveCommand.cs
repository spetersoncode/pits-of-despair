using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.ItemProperties;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to spawn items directly into player inventory.
/// Usage: give [itemId] OR give ring [property]
/// </summary>
public class GiveCommand : DebugCommand
{
    private static readonly string[] RingProperties = {
        "evasion", "regen", "armor", "max_health", "thorns",
        "resistance_fire", "resistance_cold", "resistance_poison",
        "see_invisible", "free_action"
    };

    public override string Name => "give";
    public override string Description => "Spawn an item in player inventory";
    public override string Usage => "give [itemId] OR give ring [property]";

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        // First argument: item IDs + "ring"
        if (argIndex == 0)
        {
            var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
            if (dataLoader == null)
            {
                return new[] { "ring" };
            }

            var allItems = dataLoader.Items.GetAllIds().OrderBy(id => id).ToList();
            allItems.Insert(0, "ring"); // Add ring option at the beginning

            if (string.IsNullOrEmpty(currentValue))
            {
                return allItems;
            }

            var lowerValue = currentValue.ToLower();
            return allItems.Where(id => id.Contains(lowerValue, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Second argument: if first arg is "ring", suggest ring properties
        if (argIndex == 1)
        {
            if (string.IsNullOrEmpty(currentValue))
            {
                return RingProperties;
            }

            var lowerValue = currentValue.ToLower();
            return RingProperties.Where(p => p.Contains(lowerValue, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        return null;
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                $"Usage: {Usage}",
                Palette.ToHex(Palette.Danger)
            );
        }

        var player = context.ActionContext.Player;
        var inventory = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");

        if (inventory == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no inventory component!",
                Palette.ToHex(Palette.Danger)
            );
        }

        string itemId = args[0].ToLower();

        // Handle ring generation
        if (itemId == "ring")
        {
            return ExecuteRingCommand(context, args, inventory);
        }

        // Regular item handling
        var itemData = context.DataLoader?.Items.Get(itemId);
        GD.Print($"GiveCommand: Got itemData for '{itemId}': {(itemData != null ? "found" : "null")}");
        int quantity = itemData?.Type?.ToLower() == "ammo" ? 50 : 1;

        var item = context.ActionContext.EntityFactory.CreateItem(itemId, player.GridPosition, quantity);
        GD.Print($"GiveCommand: CreateItem returned: {(item != null ? "entity" : "null")}");

        if (item == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown item ID: {itemId}",
                Palette.ToHex(Palette.Danger)
            );
        }

        GD.Print($"GiveCommand: item.ItemData is {(item.ItemData != null ? "present" : "null")}");
        var key = inventory.AddItem(item.ItemData, out string message, excludeEquipped: true);
        GD.Print($"GiveCommand: AddItem returned key={key}, message={message}");

        if (key != null)
        {
            context.ActionContext.EntityManager.RemoveEntity(item);
            item.QueueFree();

            string itemName = item.ItemData.Template.GetDisplayName(quantity);
            return DebugCommandResult.CreateSuccess(
                $"Spawned [b]{itemName}[/b] in inventory.",
                Palette.ToHex(Palette.Success)
            );
        }
        else
        {
            context.ActionContext.EntityManager.RemoveEntity(item);
            item.QueueFree();

            return DebugCommandResult.CreateFailure(
                message,
                Palette.ToHex(Palette.Caution)
            );
        }
    }

    private DebugCommandResult ExecuteRingCommand(DebugContext context, string[] args, InventoryComponent inventory)
    {
        if (args.Length < 2)
        {
            return DebugCommandResult.CreateFailure(
                $"Usage: give ring [property]\nProperties: {string.Join(", ", RingProperties)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        string propertyType = args[1].ToLower();

        // Validate property type
        if (!RingProperties.Contains(propertyType))
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown ring property: {propertyType}\nValid properties: {string.Join(", ", RingProperties)}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Get metadata for color override
        var metadata = ItemPropertyFactory.GetMetadata(propertyType);

        // Create the property
        int amount = metadata?.MinAmount ?? 1;
        var property = ItemPropertyFactory.Create(propertyType, amount, "permanent", "debug");

        if (property == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Failed to create property: {propertyType}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Create ring entity with property
        var player = context.ActionContext.Player;
        var ringEntity = context.ActionContext.EntityFactory.CreateRingWithProperty(
            property,
            player.GridPosition,
            metadata?.ColorOverride
        );

        if (ringEntity?.ItemData == null)
        {
            return DebugCommandResult.CreateFailure(
                "Failed to create ring entity!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Add to inventory
        var key = inventory.AddItem(ringEntity.ItemData, out string message, excludeEquipped: true);

        if (key != null)
        {
            ringEntity.QueueFree();

            string ringName = ringEntity.ItemData.GetDisplayName();
            return DebugCommandResult.CreateSuccess(
                $"Spawned [b]{ringName}[/b] in inventory.",
                Palette.ToHex(Palette.Success)
            );
        }
        else
        {
            ringEntity.QueueFree();

            return DebugCommandResult.CreateFailure(
                message,
                Palette.ToHex(Palette.Caution)
            );
        }
    }
}
