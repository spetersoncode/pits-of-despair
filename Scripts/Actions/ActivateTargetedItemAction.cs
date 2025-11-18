using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Text;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for using (activating) a targeted item from the player's inventory.
/// Applies the item's effects to a specific target entity and removes it if it's a consumable.
/// </summary>
public class ActivateTargetedItemAction : Action
{
	private readonly char _itemKey;
	private readonly GridPosition _targetPosition;

	public override string Name => "ActivateTargetedItem";

	public ActivateTargetedItemAction(char itemKey, GridPosition targetPosition)
	{
		_itemKey = itemKey;
		_targetPosition = targetPosition;
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

		// Check if item is equipped (can't activate equipped items)
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
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

		var itemTemplate = slot.Item.Template;

		// Check if item is equipped (can't activate equipped items)
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null && equipComponent.IsEquipped(_itemKey))
		{
			return ActionResult.CreateFailure($"{itemTemplate.Name} is equipped. Unequip it first.");
		}

		// Check if the item is activatable
		if (!itemTemplate.IsActivatable())
		{
			return ActionResult.CreateFailure($"You can't activate {itemTemplate.Name}.");
		}

		// Check if charged item has charges remaining
		if (itemTemplate.GetMaxCharges() > 0 && slot.Item.CurrentCharges <= 0)
		{
			return ActionResult.CreateFailure($"The {itemTemplate.Name} has no charges remaining.");
		}

		// Get the target entity (if any)
		var targetEntity = context.EntityManager.GetEntityAtPosition(_targetPosition);

		// If no entity at target position, fail gracefully
		if (targetEntity == null)
		{
			return ActionResult.CreateFailure("No target at that location.");
		}

		// Get the item's effects
		var effects = itemTemplate.GetEffects();
		if (effects.Count == 0)
		{
			return ActionResult.CreateFailure($"{itemTemplate.Name} has no effects.");
		}

		// Apply all effects to the target entity (not the player!)
		var messages = new StringBuilder();
		bool anyEffectSucceeded = false;

		foreach (var effect in effects)
		{
			var effectResult = effect.Apply(targetEntity, context);
			if (effectResult.Success)
			{
				anyEffectSucceeded = true;
			}

			if (!string.IsNullOrEmpty(effectResult.Message))
			{
				messages.AppendLine(effectResult.Message);
			}
		}

		// If no effects succeeded, don't consume the item or charge
		if (!anyEffectSucceeded)
		{
			return ActionResult.CreateFailure(messages.ToString().TrimEnd());
		}

		// Handle consumption based on item type
		if (itemTemplate.GetIsConsumable())
		{
			// Consumable: remove one from stack
			player.RemoveItemFromInventory(_itemKey, 1);
		}
		else if (itemTemplate.GetMaxCharges() > 0)
		{
			// Charged item: use one charge
			slot.Item.UseCharge();
		}

		// Emit feedback signal for logging
		player.EmitItemUsed(itemTemplate.Name, true, messages.ToString().TrimEnd());

		return ActionResult.CreateSuccess(messages.ToString().TrimEnd());
	}
}
