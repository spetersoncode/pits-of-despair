using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Text;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for creatures (non-players) to use items from their inventory.
/// Applies the item's effects and handles consumption/charges.
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

        var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return false;
        }

        var slot = inventory.GetSlot(_itemKey);
        if (slot == null)
        {
            return false;
        }

        // Check if item is equipped (can't activate equipped items)
        var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent != null && equipComponent.IsEquipped(_itemKey))
        {
            return false;
        }

        // Check if the item is activatable
        if (!slot.Item.Template.IsActivatable())
        {
            return false;
        }

        // If it's a charged item, check if it has charges remaining
        if (slot.Item.Template.GetMaxCharges() > 0 && slot.Item.CurrentCharges <= 0)
        {
            return false;
        }

        return true;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return ActionResult.CreateFailure("No inventory.");
        }

        var slot = inventory.GetSlot(_itemKey);
        if (slot == null)
        {
            return ActionResult.CreateFailure("No item in that slot.");
        }

        var itemTemplate = slot.Item.Template;
        string itemName = itemTemplate.GetDisplayName(1);

        // Check if item is equipped
        var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent != null && equipComponent.IsEquipped(_itemKey))
        {
            return ActionResult.CreateFailure($"{itemName} is equipped.");
        }

        // Check if the item is activatable
        if (!itemTemplate.IsActivatable())
        {
            return ActionResult.CreateFailure($"Can't use {itemName}.");
        }

        // Check charges
        if (itemTemplate.GetMaxCharges() > 0 && slot.Item.CurrentCharges <= 0)
        {
            return ActionResult.CreateFailure($"{itemName} has no charges.");
        }

        // Get the item's effects
        var effects = itemTemplate.GetEffects();
        if (effects.Count == 0)
        {
            return ActionResult.CreateFailure($"{itemName} has no effects.");
        }

        // Apply all effects
        var messages = new StringBuilder();
        bool anyEffectSucceeded = false;

        foreach (var effect in effects)
        {
            var effectResult = effect.Apply(actor, context);
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

        // Handle consumption based on item type
        if (itemTemplate.GetIsConsumable())
        {
            inventory.RemoveItem(_itemKey, 1);
        }
        else if (itemTemplate.GetMaxCharges() > 0)
        {
            slot.Item.UseCharge();
        }

        return ActionResult.CreateSuccess(messages.ToString().TrimEnd());
    }
}
