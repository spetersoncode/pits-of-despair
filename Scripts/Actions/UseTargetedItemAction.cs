using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Text;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for using (activating) a targeted item from an entity's inventory.
/// Applies the item's effects to a specific target entity and handles consumption/charges.
/// Works for both players and creatures.
/// </summary>
public class UseTargetedItemAction : Action
{
	private readonly char _itemKey;
	private readonly GridPosition _targetPosition;

	public override string Name => "UseTargetedItem";

	public UseTargetedItemAction(char itemKey, GridPosition targetPosition)
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
			return ActionResult.CreateFailure($"{itemName} has no effects.");
		}

		// Apply all effects to the target entity using unified context
		// Actor is the user (caster), targetEntity is the target
		var effectContext = EffectContext.ForItem(targetEntity, actor, context);
		var messages = new StringBuilder();
		bool anyEffectSucceeded = false;

		foreach (var effect in effects)
		{
			var effectResult = effect.Apply(effectContext);
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

		string resultMessage = messages.ToString().TrimEnd();

		// Emit player-specific feedback for UI logging
		if (actor is Player player)
		{
			player.EmitItemUsed(itemName, true, resultMessage);
		}

		return ActionResult.CreateSuccess(resultMessage);
	}
}
