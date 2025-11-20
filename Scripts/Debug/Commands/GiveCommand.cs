using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to spawn items directly into player inventory.
/// </summary>
public class GiveCommand : DebugCommand
{
    public override string Name => "give";
    public override string Description => "Spawn an item in player inventory";
    public override string Usage => "give [itemId]";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: give [itemId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string itemId = args[0];
        var player = context.Player;
        var inventory = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");

        if (inventory == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no inventory component!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Create item at player's position
        var item = context.EntityFactory.CreateItem(itemId, player.GridPosition);

        if (item == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown item ID: {itemId}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Add to inventory (use out parameter for message)
        var key = inventory.AddItem(item.ItemData, out string message, excludeEquipped: true);

        if (key != null)
        {
            // Success - remove item from world
            context.EntityManager.RemoveEntity(item);
            item.QueueFree();

            string itemName = item.ItemData.Template.GetDisplayName(1);
            return DebugCommandResult.CreateSuccess(
                $"Spawned [b]{itemName}[/b] in inventory.",
                Palette.ToHex(Palette.Success)
            );
        }
        else
        {
            // Inventory full, clean up the item
            context.EntityManager.RemoveEntity(item);
            item.QueueFree();

            return DebugCommandResult.CreateFailure(
                message,
                Palette.ToHex(Palette.Caution)
            );
        }
    }
}
