using PitsOfDespair.Entities;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for picking up an item at the actor's current position.
/// Currently player-only, as only the player has inventory.
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

        // Only players can pick up items (for now)
        if (actor is not Player)
        {
            return false;
        }

        // Check if there's an item at the actor's position
        var entityAtPosition = context.EntityManager.GetEntityAtPosition(actor.GridPosition);
        var itemAtPosition = entityAtPosition as Item;

        return itemAtPosition != null;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (actor is not Player player)
        {
            return ActionResult.CreateFailure("Only the player can pick up items.");
        }

        // Check for item at player's current position
        var entityAtPosition = context.EntityManager.GetEntityAtPosition(player.GridPosition);
        var itemAtPosition = entityAtPosition as Item;

        if (itemAtPosition == null)
        {
            string message = "Nothing to pick up.";
            player.EmitItemPickupFeedback("", false, message);
            return ActionResult.CreateFailure(message);
        }

        // Try to add to inventory
        if (!player.AddItemToInventory(itemAtPosition, out string resultMessage))
        {
            player.EmitItemPickupFeedback(itemAtPosition.DisplayName, false, resultMessage);
            return ActionResult.CreateFailure(resultMessage);
        }

        // Remove item from world
        context.EntityManager.RemoveEntity(itemAtPosition);
        itemAtPosition.QueueFree();

        // Emit feedback
        player.EmitItemPickupFeedback(itemAtPosition.DisplayName, true, resultMessage);

        return ActionResult.CreateSuccess(resultMessage);
    }
}
