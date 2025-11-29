using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Targeting;
using System.Collections.Generic;
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

		// Get the item's effects
		var effects = itemTemplate.GetEffects();
		if (effects.Count == 0)
		{
			return ActionResult.CreateFailure($"{itemName} has no effects.");
		}

		// Get targeting definition
		var definition = TargetingDefinition.FromItem(itemTemplate);
		var targetingType = definition.Type;

		// For creature targeting, require entity at target position
		if (targetingType == TargetingType.Creature)
		{
			var targetEntity = context.EntityManager.GetEntityAtPosition(_targetPosition);
			if (targetEntity == null)
			{
				return ActionResult.CreateFailure("No target at that location.");
			}
		}

		// Standard flow: get targets and apply effects (effects handle their own visuals)
		var messages = new StringBuilder();
		bool anyEffectSucceeded = false;

		// Get affected targets using targeting handler
		var handler = TargetingHandler.CreateForDefinition(definition);
		var targets = handler.GetAffectedEntities(actor, _targetPosition, definition, context);

		// Apply all effects to targets (effects handle their own visuals via VisualConfig)
		foreach (var effect in effects)
		{
			// Apply effect to targets, passing target position for tile-based effects
			var effectResults = effect.ApplyToTargets(actor, targets, context, _targetPosition);

			foreach (var effectResult in effectResults)
			{
				if (effectResult.Success)
				{
					anyEffectSucceeded = true;
				}
				if (!string.IsNullOrEmpty(effectResult.Message))
				{
					messages.AppendLine(effectResult.Message);
				}
			}

			// Add miss message for positional targeting with no hits
			if (effectResults.Count == 0 && IsPositionalTargeting(targetingType))
			{
				messages.AppendLine("The effect affects nothing.");
				anyEffectSucceeded = true;
			}
		}

		// If no effects succeeded, don't consume the item
		if (!anyEffectSucceeded)
		{
			return ActionResult.CreateFailure(messages.ToString().TrimEnd());
		}

		// Handle consumption
		HandleItemConsumption(inventory, slot, itemTemplate, messages);

		string resultMessage = messages.ToString().TrimEnd();

		// Emit player-specific feedback for UI logging
		if (actor is Player player)
		{
			player.EmitItemUsed(itemName, true, resultMessage);
		}

		return ActionResult.CreateSuccess(resultMessage);
	}

	/// <summary>
	/// Handles item consumption and charge depletion.
	/// </summary>
	private void HandleItemConsumption(InventoryComponent inventory, InventorySlot slot, ItemData itemTemplate, StringBuilder messages)
	{
		if (itemTemplate.GetIsConsumable())
		{
			inventory.RemoveItem(_itemKey, 1);
		}
		else if (itemTemplate.GetMaxCharges() > 0)
		{
			slot.Item.UseCharge();

			if (slot.Item.CurrentCharges <= 0)
			{
				bool isStaff = itemTemplate.Type?.ToLower() == "staff";
				if (!isStaff)
				{
					messages.AppendLine("The wand crumbles to dust.");
					inventory.RemoveItem(_itemKey, 1);
				}
			}
		}
	}

	private static bool IsPositionalTargeting(TargetingType type)
	{
		return type == TargetingType.Line || type == TargetingType.Area || type == TargetingType.Cone || type == TargetingType.Tile;
	}
}
