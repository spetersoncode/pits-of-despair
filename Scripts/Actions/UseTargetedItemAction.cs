using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Systems.VisualEffects;
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

		// Determine targeting type
		var targetingType = itemTemplate.Targeting?.Type?.ToLower() ?? "";

		var messages = new StringBuilder();
		bool anyEffectSucceeded = false;

		if (targetingType == "line")
		{
			// Line-targeted item (tunneling, lightning bolt, etc.)
			anyEffectSucceeded = ApplyLineEffects(actor, effects, context, messages);
		}
		else if (targetingType == "area")
		{
			// AOE item: apply effects to all entities in the area
			anyEffectSucceeded = ApplyAreaEffects(actor, effects, context, messages);
		}
		else if (targetingType == "cone")
		{
			// Cone-targeted item (cone of cold, fire breath, etc.)
			anyEffectSucceeded = ApplyConeEffects(actor, effects, context, messages);
		}
		else if (targetingType == "tile")
		{
			// Tile-targeted item (poison cloud, etc.) - doesn't require entity
			anyEffectSucceeded = ApplyTileEffects(actor, effects, context, messages);
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

			// Check if charged item is depleted
			if (slot.Item.CurrentCharges <= 0)
			{
				// Wands crumble when depleted, staves remain (they recharge)
				bool isStaff = itemTemplate.Type?.ToLower() == "staff";
				if (!isStaff)
				{
					// Wand crumbles to dust
					messages.AppendLine("The wand crumbles to dust.");
					inventory.RemoveItem(_itemKey, 1);
				}
				// Staves just become temporarily unusable until recharged
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
	/// Applies effects along a line from caster to target position.
	/// Handles tunneling and other line-based effects with beam visual.
	/// </summary>
	private bool ApplyLineEffects(BaseEntity actor, System.Collections.Generic.List<Effect> effects, ActionContext context, StringBuilder messages)
	{
		// Get targeting definition for range
		var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		var slot = inventory?.GetSlot(_itemKey);
		if (slot == null) return false;

		var definition = TargetingDefinition.FromItem(slot.Item.Template);
		int range = definition.Range > 0 ? definition.Range : 8;

		bool anySucceeded = false;

		foreach (var effect in effects)
		{
			if (effect is TunnelingEffect tunnelingEffect)
			{
				// Apply tunneling effect with range from targeting definition
				var result = tunnelingEffect.ApplyToLine(actor, _targetPosition, range, context);

				if (result.Success)
				{
					anySucceeded = true;
				}

				if (!string.IsNullOrEmpty(result.Message))
				{
					messages.AppendLine(result.Message);
				}

				// Spawn beam visual effect
				if (context.VisualEffectSystem != null)
				{
					// Calculate actual beam end position (may stop at boundary)
					var endPos = tunnelingEffect.GetBeamEndPosition(actor, _targetPosition, range, context);
					context.VisualEffectSystem.SpawnBeam(actor.GridPosition, endPos, Palette.Ochre, 0.5f);
				}
			}
			else
			{
				// Other line effects (future: lightning bolt, etc.)
				// Apply to all entities along the line using the already-retrieved definition
				var handler = new LineTargetingHandler();
				var targets = handler.GetAffectedEntities(actor, _targetPosition, definition, context);
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

				// If no targets but effect was valid, still count as success for terrain effects
				if (targets.Count == 0)
				{
					anySucceeded = true;
				}
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
		if (fireballEffect != null && context.VisualEffectSystem != null)
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

			// Spawn the fireball projectile via VFX system
			context.VisualEffectSystem.SpawnProjectile(
				VisualEffectDefinitions.FireballProjectile,
				actor.GridPosition,
				_targetPosition,
				onImpact);

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

	/// <summary>
	/// Applies effects in a cone from caster toward target position.
	/// Handles cone of cold and other cone-based effects.
	/// </summary>
	private bool ApplyConeEffects(BaseEntity actor, System.Collections.Generic.List<Effect> effects, ActionContext context, StringBuilder messages)
	{
		var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		var slot = inventory?.GetSlot(_itemKey);
		if (slot == null) return false;

		var definition = TargetingDefinition.FromItem(slot.Item.Template);
		bool anySucceeded = false;

		foreach (var effect in effects)
		{
			if (effect is ConeOfColdEffect coneEffect)
			{
				// Use targeting definition values
				coneEffect.Range = definition.Range > 0 ? definition.Range : coneEffect.Range;
				coneEffect.Radius = definition.Radius > 0 ? definition.Radius : coneEffect.Radius;

				var results = coneEffect.ApplyToCone(actor, _targetPosition, context);
				foreach (var result in results)
				{
					if (result.Success)
					{
						anySucceeded = true;
					}
					if (!string.IsNullOrEmpty(result.Message))
					{
						messages.AppendLine(result.Message);
					}
				}

				if (results.Count == 0)
				{
					messages.AppendLine("The freezing blast hits nothing.");
					anySucceeded = true;
				}

				// Spawn cone of cold visual effect
				context.VisualEffectSystem?.SpawnConeOfCold(
					actor.GridPosition,
					_targetPosition,
					coneEffect.Range,
					coneEffect.Radius);
			}
			else
			{
				// Generic cone effect - get affected entities via handler
				var handler = new ConeTargetingHandler();
				var targets = handler.GetAffectedEntities(actor, _targetPosition, definition, context);

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

				if (targets.Count == 0)
				{
					anySucceeded = true;
				}
			}
		}

		return anySucceeded;
	}

	/// <summary>
	/// Applies effects to a tile position (doesn't require an entity).
	/// Used for tile hazards like poison cloud.
	/// </summary>
	private bool ApplyTileEffects(BaseEntity actor, System.Collections.Generic.List<Effect> effects, ActionContext context, StringBuilder messages)
	{
		var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		var slot = inventory?.GetSlot(_itemKey);
		if (slot == null) return false;

		var definition = TargetingDefinition.FromItem(slot.Item.Template);
		bool anySucceeded = false;

		foreach (var effect in effects)
		{
			if (effect is CreateHazardEffect hazardEffect)
			{
				// Set target position and radius from targeting definition
				hazardEffect.TargetPosition = _targetPosition;
				hazardEffect.Radius = definition.Radius > 0 ? definition.Radius : hazardEffect.Radius;

				// Create a dummy context with no specific target
				var effectContext = EffectContext.ForItem(actor, actor, context);
				var effectResult = hazardEffect.Apply(effectContext);

				if (effectResult.Success)
				{
					anySucceeded = true;
				}
				if (!string.IsNullOrEmpty(effectResult.Message))
				{
					messages.AppendLine(effectResult.Message);
				}
			}
			else
			{
				// Other tile effects - apply to any entity at position if present
				var targetEntity = context.EntityManager.GetEntityAtPosition(_targetPosition);
				if (targetEntity != null)
				{
					var effectContext = EffectContext.ForItem(targetEntity, actor, context);
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
				else
				{
					// No entity at tile - effect still "succeeds" for tile-targeting
					anySucceeded = true;
				}
			}
		}

		return anySucceeded;
	}
}
