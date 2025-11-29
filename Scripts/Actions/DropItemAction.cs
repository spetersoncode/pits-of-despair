using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for dropping an item from an entity's inventory.
/// Removes the item from inventory and spawns it at the entity's position.
/// Works for both players and creatures.
/// </summary>
public class DropItemAction : Action
{
	private readonly char _itemKey;

	public override string Name => "DropItem";

	public DropItemAction(char itemKey)
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

		// Check if the item exists in inventory
		var slot = inventory.GetSlot(_itemKey);
		return slot != null;
	}

	public override ActionResult Execute(BaseEntity actor, ActionContext context)
	{
		var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		if (inventory == null)
		{
			return ActionResult.CreateFailure("No inventory.");
		}

		// Get the inventory slot
		var slot = inventory.GetSlot(_itemKey);
		if (slot == null)
		{
			return ActionResult.CreateFailure("No item in that slot.");
		}

		// Clone the item instance before removing from inventory
		// This ensures the dropped entity doesn't share state with inventory items
		var itemInstance = slot.Item.Clone();
		itemInstance.AutoPickup = false; // Disable autopickup for dropped items
		string itemName = itemInstance.Template.GetDisplayName(1);

		// Determine drop position BEFORE removing from inventory
		GridPosition dropPosition = actor.GridPosition;
		bool droppedNearby = false;

		// Check if there's already an item at the actor's position
		var entitiesAtActorPos = context.EntityManager.GetEntitiesAtPosition(actor.GridPosition);
		bool hasItemAtPosition = entitiesAtActorPos.Any(e => e.ItemData != null);

		if (hasItemAtPosition)
		{
			// Find nearest empty tile
			var emptyTile = FindNearestEmptyTile(actor.GridPosition, context);
			if (emptyTile.HasValue)
			{
				dropPosition = emptyTile.Value;
				droppedNearby = true;
			}
			else
			{
				// No empty tiles available - fail without removing from inventory
				string failureMessage = $"No room to drop {itemName}.";
				if (actor is Player player)
				{
					player.EmitItemDropped(itemName, false, failureMessage);
				}
				return ActionResult.CreateFailure(failureMessage);
			}
		}

		// If item is equipped, unequip it first
		var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null && equipComponent.IsEquipped(_itemKey))
		{
			var equipSlot = equipComponent.GetEquippedSlotForItem(_itemKey);
			equipComponent.Unequip(equipSlot);
		}

		// Remove one item from inventory
		if (!inventory.RemoveItem(_itemKey, 1))
		{
			return ActionResult.CreateFailure($"Failed to remove {itemName} from inventory.");
		}

		// Create item entity using factory (preserves ItemInstance state)
		var itemEntity = context.EntityFactory.CreateItemFromInstance(itemInstance, dropPosition);

		// Add to entity manager
		context.EntityManager.AddEntity(itemEntity);

		// Determine success message
		string successMessage = droppedNearby
			? $"Dropped {itemName} nearby."
			: $"Dropped {itemName}.";

		// Emit player-specific feedback for UI logging
		if (actor is Player p)
		{
			p.EmitItemDropped(itemName, true, successMessage);
		}

		// Return success with appropriate message
		return ActionResult.CreateSuccess(successMessage);
	}

	/// <summary>
	/// Finds the nearest empty adjacent tile (no items, walkable).
	/// Searches in 8 directions clockwise starting from north.
	/// </summary>
	private GridPosition? FindNearestEmptyTile(GridPosition origin, ActionContext context)
	{
		// Check adjacent tiles in clockwise order: N, NE, E, SE, S, SW, W, NW
		var offsets = new[]
		{
			new GridPosition(0, -1),   // North
			new GridPosition(1, -1),   // Northeast
			new GridPosition(1, 0),    // East
			new GridPosition(1, 1),    // Southeast
			new GridPosition(0, 1),    // South
			new GridPosition(-1, 1),   // Southwest
			new GridPosition(-1, 0),   // West
			new GridPosition(-1, -1)   // Northwest
		};

		foreach (var offset in offsets)
		{
			var checkPos = new GridPosition(origin.X + offset.X, origin.Y + offset.Y);

			// Check if tile is walkable
			if (!context.MapSystem.IsWalkable(checkPos))
			{
				continue;
			}

			// Check if tile already has an item
			var entitiesAtPos = context.EntityManager.GetEntitiesAtPosition(checkPos);
			bool hasItem = entitiesAtPos.Any(e => e.ItemData != null);

			if (!hasItem)
			{
				return checkPos;
			}
		}

		// No empty tile found
		return null;
	}
}
