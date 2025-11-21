using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Collections.Generic;

namespace PitsOfDespair.ViewModels;

/// <summary>
/// View model that provides formatted equipment display data.
/// Listens to equipment changes and provides presentation-ready data for UI.
/// Decouples UI from InventoryComponent and ItemTemplate internals.
/// </summary>
public partial class EquipmentViewModel : Node
{
	#region Signals

	/// <summary>
	/// Emitted when equipment display data changes.
	/// UI should refresh its equipment display when this signal is received.
	/// </summary>
	[Signal]
	public delegate void EquipmentDisplayUpdatedEventHandler();

	#endregion

	#region Data Classes

	/// <summary>
	/// Display data for a single equipment slot.
	/// </summary>
	public class EquipmentSlotData
	{
		public string SlotName { get; set; }
		public bool IsEquipped { get; set; }
		public string ItemName { get; set; }
		public string ItemGlyph { get; set; }
		public Color ItemColor { get; set; }

		public EquipmentSlotData(string slotName)
		{
			SlotName = slotName;
			IsEquipped = false;
			ItemName = "(none)";
			ItemGlyph = "";
			ItemColor = Colors.Gray;
		}
	}

	#endregion

	#region State

	private Player _player;
	private EquipComponent _equipComponent;

	#endregion

	#region Properties

	public EquipmentSlotData MeleeWeapon { get; private set; }
	public EquipmentSlotData RangedWeapon { get; private set; }
	public EquipmentSlotData Armor { get; private set; }
	public EquipmentSlotData Ring1 { get; private set; }
	public EquipmentSlotData Ring2 { get; private set; }

	/// <summary>
	/// Gets all equipment slots in display order.
	/// </summary>
	public IEnumerable<EquipmentSlotData> AllSlots
	{
		get
		{
			yield return MeleeWeapon;
			yield return RangedWeapon;
			yield return Armor;
			yield return Ring1;
			yield return Ring2;
		}
	}

	#endregion

	#region Initialization

	public EquipmentViewModel()
	{
		// Initialize slot data
		MeleeWeapon = new EquipmentSlotData("Melee");
		RangedWeapon = new EquipmentSlotData("Ranged");
		Armor = new EquipmentSlotData("Armor");
		Ring1 = new EquipmentSlotData("Ring");
		Ring2 = new EquipmentSlotData("Ring");
	}

	/// <summary>
	/// Initializes the view model with the player entity.
	/// </summary>
	/// <param name="player">The player entity</param>
	public void Initialize(Player player)
	{
		if (player == null)
		{
			GD.PushError("EquipmentViewModel: Cannot initialize with null player.");
			return;
		}

		_player = player;
		_equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

		if (_equipComponent == null)
		{
			GD.PushError("EquipmentViewModel: Player missing EquipComponent.");
			return;
		}

		// Connect to signals
		ConnectToSignals();

		// Initialize display data with current equipment
		UpdateAllEquipment();
	}

	#endregion

	#region Signal Connection

	private void ConnectToSignals()
	{
		// Equipment changes
		_equipComponent.Connect(
			EquipComponent.SignalName.EquipmentChanged,
			Callable.From<EquipmentSlot>(OnEquipmentChanged)
		);

		// Inventory changes (in case item properties change)
		_player.Connect(
			Player.SignalName.InventoryChanged,
			Callable.From(OnInventoryChanged)
		);
	}

	#endregion

	#region Update Methods

	/// <summary>
	/// Updates all equipment slots.
	/// </summary>
	private void UpdateAllEquipment()
	{
		UpdateSlot(EquipmentSlot.MeleeWeapon, MeleeWeapon);
		UpdateSlot(EquipmentSlot.RangedWeapon, RangedWeapon);
		UpdateSlot(EquipmentSlot.Armor, Armor);
		UpdateSlot(EquipmentSlot.Ring1, Ring1);
		UpdateSlot(EquipmentSlot.Ring2, Ring2);
	}

	/// <summary>
	/// Updates a specific equipment slot's display data.
	/// </summary>
	private void UpdateSlot(EquipmentSlot slot, EquipmentSlotData slotData)
	{
		var equippedKey = _equipComponent.GetEquippedKey(slot);

		if (equippedKey.HasValue)
		{
			var inventorySlot = _player.GetInventorySlot(equippedKey.Value);
			if (inventorySlot != null)
			{
				slotData.IsEquipped = true;
				slotData.ItemName = inventorySlot.Item.Template.Name;
				slotData.ItemGlyph = inventorySlot.Item.Template.GetGlyph();
				slotData.ItemColor = inventorySlot.Item.Template.GetColor();
			}
			else
			{
				// Equipment reference exists but inventory slot not found (error state)
				slotData.IsEquipped = false;
				slotData.ItemName = "(error)";
				slotData.ItemGlyph = "?";
				slotData.ItemColor = Colors.Red;
			}
		}
		else
		{
			// No item equipped in this slot
			slotData.IsEquipped = false;
			slotData.ItemName = "(none)";
			slotData.ItemGlyph = "";
			slotData.ItemColor = Colors.Gray;
		}
	}

	#endregion

	#region Event Handlers

	private void OnEquipmentChanged(EquipmentSlot slot)
	{
		// Update only the changed slot
		var slotData = slot switch
		{
			EquipmentSlot.MeleeWeapon => MeleeWeapon,
			EquipmentSlot.RangedWeapon => RangedWeapon,
			EquipmentSlot.Armor => Armor,
			EquipmentSlot.Ring1 => Ring1,
			EquipmentSlot.Ring2 => Ring2,
			_ => null
		};

		if (slotData != null)
		{
			UpdateSlot(slot, slotData);
			EmitSignal(SignalName.EquipmentDisplayUpdated);
		}
	}

	private void OnInventoryChanged()
	{
		// Inventory changed, equipment display might need updating
		// (e.g., if item was dropped/picked up, properties changed)
		UpdateAllEquipment();
		EmitSignal(SignalName.EquipmentDisplayUpdated);
	}

	#endregion

	#region Cleanup

	public override void _ExitTree()
	{
		// Disconnect from equipment component signal
		if (_equipComponent != null)
		{
			_equipComponent.Disconnect(
				EquipComponent.SignalName.EquipmentChanged,
				Callable.From<EquipmentSlot>(OnEquipmentChanged)
			);
		}

		// Disconnect from player signal
		if (_player != null)
		{
			_player.Disconnect(
				Player.SignalName.InventoryChanged,
				Callable.From(OnInventoryChanged)
			);
		}
	}

	#endregion
}
