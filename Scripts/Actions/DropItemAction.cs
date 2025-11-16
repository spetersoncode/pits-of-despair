using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for dropping an item from the player's inventory.
/// Removes the item from inventory and spawns it at the player's position.
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

		// Only players can drop items from inventory
		if (actor is not Player player)
		{
			return false;
		}

		// Check if the item exists in inventory
		var slot = player.GetInventorySlot(_itemKey);
		return slot != null;
	}

	public override ActionResult Execute(BaseEntity actor, ActionContext context)
	{
		if (actor is not Player player)
		{
			return ActionResult.CreateFailure("Only the player can drop items.");
		}

		// Get the inventory slot
		var slot = player.GetInventorySlot(_itemKey);
		if (slot == null)
		{
			return ActionResult.CreateFailure("No item in that slot.");
		}

		// Store item data before removing from inventory
		var itemData = slot.ItemData;
		string itemName = itemData.Name;

		// Remove one item from inventory
		if (!player.RemoveItemFromInventory(_itemKey, 1))
		{
			return ActionResult.CreateFailure($"Failed to remove {itemName} from inventory.");
		}

		// Create the item entity at player's position
		var itemEntity = new BaseEntity
		{
			GridPosition = player.GridPosition,
			DisplayName = itemData.Name,
			Glyph = !string.IsNullOrEmpty(itemData.Glyph) ? itemData.Glyph : "?",
			GlyphColor = itemData.GetColor(),
			Name = itemData.Name
		};

		// Add ItemComponent
		var itemComponent = new ItemComponent
		{
			Name = "ItemComponent",
			ItemData = itemData
		};
		itemEntity.AddChild(itemComponent);

		// Add to entity manager
		context.EntityManager.AddEntity(itemEntity);

		// Emit feedback signal for logging
		player.EmitItemDropped(itemName);

		return ActionResult.CreateSuccess($"You drop {itemName}.");
	}
}
