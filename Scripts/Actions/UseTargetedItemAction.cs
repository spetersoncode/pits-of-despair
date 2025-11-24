using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Systems.Projectiles;
using PitsOfDespair.Targeting;
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

		// Determine if this is an area-targeted item
		bool isAreaTargeted = itemTemplate.Targeting?.Type?.ToLower() == "area";

		var messages = new StringBuilder();
		bool anyEffectSucceeded = false;

		if (isAreaTargeted)
		{
			// AOE item: apply effects to all entities in the area
			anyEffectSucceeded = ApplyAreaEffects(actor, effects, context, messages);
		}
		else
		{
			// Single-target item: require entity at target position
			var targetEntity = context.EntityManager.GetEntityAtPosition(_targetPosition);
			if (targetEntity == null)
			{
				return ActionResult.CreateFailure("No target at that location.");
			}
			anyEffectSucceeded = ApplySingleTargetEffects(actor, targetEntity, effects, context, messages);
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

			// Check if wand is depleted
			if (slot.Item.CurrentCharges <= 0)
			{
				// Wand crumbles to dust
				messages.AppendLine("The wand crumbles to dust.");
				inventory.RemoveItem(_itemKey, 1);
			}
		}

		string resultMessage = messages.ToString().TrimEnd();

		// Emit player-specific feedback for UI logging
		if (actor is Player player)
		{
			player.EmitItemUsed(itemName, true, resultMessage);
		}

		return ActionResult.CreateSuccess(resultMessage);
	}

	/// <summary>
	/// Applies effects to a single target entity.
	/// </summary>
	private bool ApplySingleTargetEffects(BaseEntity actor, BaseEntity target, System.Collections.Generic.List<Effect> effects, ActionContext context, StringBuilder messages)
	{
		bool anySucceeded = false;
		var effectContext = EffectContext.ForItem(target, actor, context);

		foreach (var effect in effects)
		{
			var effectResult = effect.Apply(effectContext);
			if (effectResult.Success)
			{
				anySucceeded = true;
			}

			if (!string.IsNullOrEmpty(effectResult.Message))
			{
				messages.AppendLine(effectResult.Message);
			}
		}

		return anySucceeded;
	}

	/// <summary>
	/// Applies effects to all entities in the target area.
	/// Uses targeting handler to determine affected entities.
	/// For fireball effects, spawns a projectile that triggers on impact.
	/// </summary>
	private bool ApplyAreaEffects(BaseEntity actor, System.Collections.Generic.List<Effect> effects, ActionContext context, StringBuilder messages)
	{
		// Get targeting definition and handler for the item
		var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		var slot = inventory?.GetSlot(_itemKey);
		if (slot == null) return false;

		var definition = TargetingDefinition.FromItem(slot.Item.Template);

		// Check if any effect is a fireball (needs projectile)
		FireballEffect? fireballEffect = null;
		foreach (var effect in effects)
		{
			if (effect is FireballEffect fb)
			{
				fireballEffect = fb;
				break;
			}
		}

		// For fireball, spawn a projectile with callback
		if (fireballEffect != null && context.ProjectileSystem != null)
		{
			// Use targeting definition's radius, falling back to effect's default
			int radius = definition.Radius > 0 ? definition.Radius : fireballEffect.Radius;

			// Ensure the effect uses the same radius as targeting
			fireballEffect.Radius = radius;

			// Create callback that applies damage and spawns explosion visual on impact
			System.Action onImpact = () =>
			{
				// Apply AOE damage
				var results = fireballEffect.ApplyToArea(actor, _targetPosition, context);

				// Log messages via combat system
				foreach (var result in results)
				{
					if (!string.IsNullOrEmpty(result.Message))
					{
						context.CombatSystem?.EmitActionMessage(actor, result.Message, Palette.ToHex(Palette.Fire));
					}
				}
				if (results.Count == 0)
				{
					context.CombatSystem?.EmitActionMessage(actor, "The flames dissipate harmlessly.", Palette.ToHex(Palette.Fire));
				}

				// Spawn explosion visual
				context.VisualEffectSystem?.SpawnExplosion(_targetPosition, radius, Palette.Fire);
			};

			// Spawn the fireball projectile
			context.ProjectileSystem.SpawnProjectileWithCallback(
				actor.GridPosition,
				_targetPosition,
				ProjectileDefinitions.Fireball,
				onImpact,
				actor);

			// Projectile spawned successfully - damage will be applied on impact
			return true;
		}

		// Non-projectile AOE effects (apply immediately)
		bool anySucceeded = false;
		var handler = TargetingHandler.CreateForDefinition(definition);
		var targets = handler.GetAffectedEntities(actor, _targetPosition, definition, context);

		foreach (var effect in effects)
		{
			foreach (var target in targets)
			{
				var effectContext = EffectContext.ForItem(target, actor, context);
				var effectResult = effect.Apply(effectContext);
				if (effectResult.Success)
				{
					anySucceeded = true;
				}
				if (!string.IsNullOrEmpty(effectResult.Message))
				{
					messages.AppendLine(effectResult.Message);
				}
			}
		}

		// If no targets were hit but the effect was valid, still count as success
		if (targets.Count == 0 && effects.Count > 0)
		{
			messages.AppendLine("The effect dissipates harmlessly.");
			anySucceeded = true;
		}

		return anySucceeded;
	}
}
