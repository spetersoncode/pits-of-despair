using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;

namespace PitsOfDespair.Systems;

/// <summary>
/// Handles player action creation and execution.
/// Coordinates item usage, equipment, drops, and inventory management.
/// Determines when targeting is required vs direct activation.
/// </summary>
public partial class PlayerActionHandler : Node
{
	#region Signals

	/// <summary>
	/// Emitted when an item requires targeting before activation.
	/// Parameter: item key
	/// </summary>
	[Signal]
	public delegate void StartItemTargetingEventHandler(char itemKey);

	/// <summary>
	/// Emitted when a reach attack (equipped melee weapon with range > 1) is used.
	/// Parameter: item key
	/// </summary>
	[Signal]
	public delegate void StartReachAttackTargetingEventHandler(char itemKey);

	/// <summary>
	/// Emitted after an item key is successfully rebound.
	/// Parameter: message describing the rebind result
	/// </summary>
	[Signal]
	public delegate void ItemReboundEventHandler(string message);

	#endregion

	#region State

	private Player _player;
	private ActionContext _actionContext;
	private InventoryComponent _inventoryComponent;
	private EquipComponent _equipComponent;

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the action handler with the player entity and action context.
	/// </summary>
	/// <param name="player">The player entity</param>
	/// <param name="actionContext">Action execution context</param>
	public void Initialize(Player player, ActionContext actionContext)
	{
		if (player == null)
		{
			GD.PushError("PlayerActionHandler: Cannot initialize with null player.");
			return;
		}

		if (actionContext == null)
		{
			GD.PushError("PlayerActionHandler: Cannot initialize with null action context.");
			return;
		}

		_player = player;
		_actionContext = actionContext;

		_inventoryComponent = _player.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		_equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

		if (_inventoryComponent == null)
		{
			GD.PushWarning("PlayerActionHandler: Player missing InventoryComponent.");
		}

		if (_equipComponent == null)
		{
			GD.PushWarning("PlayerActionHandler: Player missing EquipComponent.");
		}
	}

	#endregion

	#region Item Actions

	/// <summary>
	/// Activates an item from the player's inventory.
	/// Determines if targeting is required or if the item can be used directly.
	/// </summary>
	/// <param name="key">Inventory key of the item to activate</param>
	public void ActivateItem(char key)
	{
		var slot = _player.GetInventorySlot(key);
		if (slot == null)
		{
			GD.PushWarning($"PlayerActionHandler: No item found at key '{key}'.");
			return;
		}

		bool isEquipped = _equipComponent != null && _equipComponent.IsEquipped(key);

		// Check if this is a reach weapon (equipped melee weapon with range > 1)
		if (isEquipped &&
			slot.Item.Template.Attack != null &&
			slot.Item.Template.Attack.Type == AttackType.Melee &&
			slot.Item.Template.Attack.Range > 1)
		{
			// Reach weapon - requires targeting
			EmitSignal(SignalName.StartReachAttackTargeting, key);
			return;
		}

		// Check if item requires targeting
		if (slot.Item.Template.RequiresTargeting())
		{
			// Item requires targeting (e.g., throwable, ranged)
			EmitSignal(SignalName.StartItemTargeting, key);
			return;
		}

		// Regular activation (no targeting needed)
		var action = new ActivateItemAction(key);
		_player.ExecuteAction(action, _actionContext);
	}

	/// <summary>
	/// Drops an item from the player's inventory.
	/// </summary>
	/// <param name="key">Inventory key of the item to drop</param>
	public void DropItem(char key)
	{
		var action = new DropItemAction(key);
		_player.ExecuteAction(action, _actionContext);
	}

	/// <summary>
	/// Equips or unequips an item from the player's inventory.
	/// </summary>
	/// <param name="key">Inventory key of the item to equip/unequip</param>
	public void EquipItem(char key)
	{
		var action = new EquipAction(key);
		_player.ExecuteAction(action, _actionContext);
	}

	#endregion

	#region Inventory Management

	/// <summary>
	/// Rebinds an item from one inventory key to another.
	/// Handles swapping if the target key is occupied.
	/// </summary>
	/// <param name="oldKey">Current inventory key</param>
	/// <param name="newKey">Desired inventory key</param>
	public void RebindItemKey(char oldKey, char newKey)
	{
		if (_inventoryComponent == null)
		{
			GD.PushWarning("PlayerActionHandler: Cannot rebind item, InventoryComponent is null.");
			return;
		}

		bool success = _inventoryComponent.RebindItemKey(oldKey, newKey);
		if (!success)
		{
			GD.PushWarning($"PlayerActionHandler: Failed to rebind item from '{oldKey}' to '{newKey}'.");
			return;
		}

		// Build result message
		string message = BuildRebindMessage(oldKey, newKey);
		EmitSignal(SignalName.ItemRebound, message);
	}

	/// <summary>
	/// Builds a descriptive message about the rebind operation.
	/// </summary>
	private string BuildRebindMessage(char oldKey, char newKey)
	{
		var slot = _player.GetInventorySlot(newKey);
		if (slot == null)
		{
			return $"Item rebound to '{newKey}'.";
		}

		string itemName = slot.Item.Template.Name;

		// Check if keys are the same (no actual rebind)
		if (oldKey == newKey)
		{
			return $"{itemName} remains on '{newKey}'.";
		}

		// Check if there was a swap
		var oldSlot = _player.GetInventorySlot(oldKey);
		if (oldSlot != null)
		{
			string swappedItemName = oldSlot.Item.Template.Name;
			return $"{itemName} rebound to '{newKey}' (swapped with {swappedItemName} on '{oldKey}').";
		}

		// Simple rebind (target slot was empty)
		return $"{itemName} rebound to '{newKey}'.";
	}

	#endregion
}
