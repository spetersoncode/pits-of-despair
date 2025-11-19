using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for viewing item details and performing actions like key rebinding.
/// Opened from InventoryModal when selecting an item by its hotkey.
/// </summary>
public partial class ItemDetailModal : CenterContainer
{
	[Signal]
	public delegate void KeyReboundEventHandler(char oldKey, char newKey);

	[Signal]
	public delegate void CancelledEventHandler();

	private RichTextLabel _contentLabel;
	private Player _player;
	private bool _isVisible = false;
	private InventorySlot _currentSlot;

	private enum State
	{
		Viewing,
		AwaitingRebind
	}

	private State _currentState = State.Viewing;

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Hide();
	}

	/// <summary>
	/// Connects to the player to access inventory.
	/// </summary>
	public void ConnectToPlayer(Player player)
	{
		_player = player;
	}

	/// <summary>
	/// Shows the modal with details for the specified item.
	/// </summary>
	public void ShowMenu(char itemKey)
	{
		if (_player == null)
		{
			GD.PrintErr("ItemDetailModal: Cannot show menu, player not connected");
			return;
		}

		_currentSlot = _player.GetInventorySlot(itemKey);
		if (_currentSlot == null)
		{
			GD.PrintErr($"ItemDetailModal: No item found for key '{itemKey}'");
			return;
		}

		_currentState = State.Viewing;
		_isVisible = true;
		Show();
		UpdateDisplay();
	}

	/// <summary>
	/// Hides the modal.
	/// </summary>
	public void HideMenu()
	{
		_isVisible = false;
		_currentSlot = null;
		_currentState = State.Viewing;
		Hide();
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isVisible)
		{
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			HandleKeyInput(keyEvent);
		}
	}

	private void HandleKeyInput(InputEventKey keyEvent)
	{
		// Handle based on current state
		switch (_currentState)
		{
			case State.Viewing:
				HandleViewingInput(keyEvent);
				break;
			case State.AwaitingRebind:
				HandleRebindInput(keyEvent);
				break;
		}
	}

	private void HandleViewingInput(InputEventKey keyEvent)
	{
		// Cancel on ESC
		if (keyEvent.Keycode == Key.Escape)
		{
			EmitSignal(SignalName.Cancelled);
			GetViewport().SetInputAsHandled();
			return;
		}

		// Block 'I' key to prevent inventory toggle while in detail view
		if (keyEvent.Keycode == Key.I)
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		// Enter rebind mode on '='
		if (keyEvent.Keycode == Key.Equal)
		{
			_currentState = State.AwaitingRebind;
			UpdateDisplay();
			GetViewport().SetInputAsHandled();
			return;
		}
	}

	private void HandleRebindInput(InputEventKey keyEvent)
	{
		// Cancel rebind on ESC
		if (keyEvent.Keycode == Key.Escape)
		{
			_currentState = State.Viewing;
			UpdateDisplay();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Accept a-z keys for rebinding
		if (keyEvent.Keycode >= Key.A && keyEvent.Keycode <= Key.Z)
		{
			char newKey = (char)('a' + (keyEvent.Keycode - Key.A));
			char oldKey = _currentSlot.Key;

			// Emit signal for rebinding
			EmitSignal(SignalName.KeyRebound, oldKey, newKey);

			// Return to viewing state
			_currentState = State.Viewing;

			// Update the slot reference since key may have changed
			_currentSlot = _player.GetInventorySlot(newKey);
			UpdateDisplay();

			GetViewport().SetInputAsHandled();
			return;
		}

		// Block all other keys in rebind mode (including 'I', '=', etc.)
		GetViewport().SetInputAsHandled();
	}

	private void UpdateDisplay()
	{
		if (_currentSlot == null || _contentLabel == null)
		{
			return;
		}

		var itemTemplate = _currentSlot.Item.Template;

		// Build display based on state
		string content = "";

		if (_currentState == State.Viewing)
		{
			content = BuildViewingDisplay(itemTemplate);
		}
		else if (_currentState == State.AwaitingRebind)
		{
			content = BuildRebindDisplay(itemTemplate);
		}

		_contentLabel.Text = content;
	}

	private string BuildViewingDisplay(ItemData itemTemplate)
	{
		string glyph = $"[color={itemTemplate.Color}]{itemTemplate.GetGlyph()}[/color]";
		string name = $"[color={itemTemplate.Color}]{itemTemplate.Name}[/color]";
		string keyInfo = $"[color={Palette.ToHex(Palette.Disabled)}]Hotkey:[/color] [color={Palette.ToHex(Palette.Default)}]{_currentSlot.Key}[/color]";

		// Stack count for consumables
		string countInfo = "";
		if (_currentSlot.Count > 1)
		{
			countInfo = $" [color={Palette.ToHex(Palette.AshGray)}](x{_currentSlot.Count})[/color]";
		}

		// Charges for charged items
		string chargesInfo = "";
		if (itemTemplate.GetMaxCharges() > 0)
		{
			chargesInfo = $"\n[color={Palette.ToHex(Palette.Disabled)}]Charges:[/color] [color={itemTemplate.Color}]{_currentSlot.Item.CurrentCharges}/{itemTemplate.GetMaxCharges()}[/color]";
		}

		// Equipment slot info
		string slotInfo = "";
		if (itemTemplate.GetIsEquippable())
		{
			var equipSlot = itemTemplate.GetEquipmentSlot();
			string slotName = ItemFormatter.FormatSlotName(equipSlot);
			slotInfo = $"\n[color={Palette.ToHex(Palette.Disabled)}]Slot:[/color] [color={Palette.ToHex(Palette.AshGray)}]{slotName}[/color]";
		}

		// Commands section
		string commands = $"\n\n[color={Palette.ToHex(Palette.Disabled)}]Commands:[/color]\n";
		commands += $"[color={Palette.ToHex(Palette.Default)}]=[/color] Rebind hotkey\n";
		commands += $"[color={Palette.ToHex(Palette.Default)}]ESC[/color] Close";

		return $"[center][b]Item Details[/b][/center]\n\n" +
		       $"{glyph} {name}{countInfo}\n" +
		       $"{keyInfo}{chargesInfo}{slotInfo}" +
		       $"{commands}";
	}

	private string BuildRebindDisplay(ItemData itemTemplate)
	{
		string glyph = $"[color={itemTemplate.Color}]{itemTemplate.GetGlyph()}[/color]";
		string name = $"[color={itemTemplate.Color}]{itemTemplate.Name}[/color]";
		string currentKey = $"[color={Palette.ToHex(Palette.Caution)}]{_currentSlot.Key}[/color]";

		string instructions = $"\n\n[center][color={Palette.ToHex(Palette.Caution)}]REBIND MODE[/color][/center]\n\n";
		instructions += $"Current hotkey: {currentKey}\n\n";
		instructions += $"Press [color={Palette.ToHex(Palette.Default)}]a-z[/color] to assign new hotkey\n";
		instructions += $"Press [color={Palette.ToHex(Palette.Default)}]ESC[/color] to cancel";

		return $"[center][b]Rebind Hotkey[/b][/center]\n\n" +
		       $"{glyph} {name}" +
		       $"{instructions}";
	}
}
