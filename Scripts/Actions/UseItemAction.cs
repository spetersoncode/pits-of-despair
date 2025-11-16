using PitsOfDespair.Entities;
using System.Text;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for using (activating) an item from the player's inventory.
/// Applies the item's effects and removes it if it's a consumable.
/// </summary>
public class UseItemAction : Action
{
    private readonly char _itemKey;

    public override string Name => "UseItem";

    public UseItemAction(char itemKey)
    {
        _itemKey = itemKey;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        // Only players can use items from inventory
        if (actor is not Player player)
        {
            return false;
        }

        // Check if the item exists in inventory
        var slot = player.GetInventorySlot(_itemKey);
        if (slot == null)
        {
            return false;
        }

        // Check if the item is activatable
        return slot.ItemData.IsActivatable;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (actor is not Player player)
        {
            return ActionResult.CreateFailure("Only the player can use items.");
        }

        // Get the inventory slot
        var slot = player.GetInventorySlot(_itemKey);
        if (slot == null)
        {
            return ActionResult.CreateFailure("No item in that slot.");
        }

        // Check if the item is activatable
        if (!slot.ItemData.IsActivatable)
        {
            return ActionResult.CreateFailure($"You can't activate {slot.ItemData.Name}.");
        }

        // Get the item's effects
        var effects = slot.ItemData.GetEffects();
        if (effects.Count == 0)
        {
            return ActionResult.CreateFailure($"{slot.ItemData.Name} has no effects.");
        }

        // Apply all effects
        var messages = new StringBuilder();
        bool anyEffectSucceeded = false;

        foreach (var effect in effects)
        {
            var effectResult = effect.Apply(player, context);
            if (effectResult.Success)
            {
                anyEffectSucceeded = true;
            }

            if (!string.IsNullOrEmpty(effectResult.Message))
            {
                messages.AppendLine(effectResult.Message);
            }
        }

        // If no effects succeeded, don't consume the item
        if (!anyEffectSucceeded)
        {
            return ActionResult.CreateFailure(messages.ToString().TrimEnd());
        }

        // Remove the consumed item from inventory
        player.RemoveItemFromInventory(_itemKey, 1);

        // Emit feedback signal for logging
        player.EmitItemUsed(slot.ItemData.Name, true, messages.ToString().TrimEnd());

        return ActionResult.CreateSuccess(messages.ToString().TrimEnd());
    }
}
