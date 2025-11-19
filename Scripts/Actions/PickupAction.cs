using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for picking up an item at the actor's current position.
/// Works for any entity with an InventoryComponent.
/// </summary>
public class PickupAction : Action
{
    public override string Name => "Pickup";

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        // Actor must have inventory component
        var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return false;
        }

        // Check if there's an item at the actor's position
        var entityAtPosition = context.EntityManager.GetEntityAtPosition(actor.GridPosition);

        return entityAtPosition?.ItemData != null;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        // Get actor's inventory component
        var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return ActionResult.CreateFailure("Cannot pick up items without inventory.");
        }

        // Check for item at actor's current position
        var entityAtPosition = context.EntityManager.GetEntityAtPosition(actor.GridPosition);

        if (entityAtPosition?.ItemData == null)
        {
            return ActionResult.CreateFailure("Nothing to pick up.");
        }

        // Try to add to inventory
        var key = inventory.AddItem(entityAtPosition.ItemData, out string message, excludeEquipped: true);

        if (key == null)
        {
            // Failed to add (inventory full or other issue)
            return ActionResult.CreateFailure(message);
        }

        // Remove item from world
        context.EntityManager.RemoveEntity(entityAtPosition);
        entityAtPosition.QueueFree();

        string itemName = entityAtPosition.ItemData.Template.GetDisplayName(1);
        string successMessage = $"Picked up {itemName}.";

        // Note: Core logic is generic (works with any InventoryComponent),
        // but UI feedback is player-specific since only player has message log.
        // This is acceptable as it's purely presentational, not game logic.
        if (actor is Player player)
        {
            player.EmitItemPickupFeedback(itemName, true, successMessage);
        }

        return ActionResult.CreateSuccess(successMessage);
    }
}
