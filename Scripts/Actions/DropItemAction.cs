using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
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

		// Clone the item instance before removing from inventory
		// This ensures the dropped entity doesn't share state with inventory items
		var itemInstance = slot.Item.Clone();
		string itemName = itemInstance.Template.GetDisplayName(1);

		// If item is equipped, unequip it first
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null && equipComponent.IsEquipped(_itemKey))
		{
			var equipSlot = equipComponent.GetSlotForItem(_itemKey);
			equipComponent.Unequip(equipSlot);
		}

		// Remove one item from inventory
		if (!player.RemoveItemFromInventory(_itemKey, 1))
		{
			return ActionResult.CreateFailure($"Failed to remove {itemName} from inventory.");
		}

		// Create item entity using factory (preserves ItemInstance state)
		var itemEntity = context.EntityFactory.CreateItemFromInstance(itemInstance, player.GridPosition);

		// Add to entity manager
		context.EntityManager.AddEntity(itemEntity);

		// Emit feedback signal for logging
		player.EmitItemDropped(itemName);

		return ActionResult.CreateSuccess($"You drop {itemName}.");
	}
}
