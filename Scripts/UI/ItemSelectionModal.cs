using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.UI;

/// <summary>
/// Base class for item selection modals (inventory, equip, drop, activate).
/// Provides common functionality for displaying items and handling input.
/// </summary>
public abstract partial class ItemSelectionModal : PanelContainer
{
    [Signal]
    public delegate void ItemSelectedEventHandler(char key);

    [Signal]
    public delegate void CancelledEventHandler();

    protected RichTextLabel _itemsLabel;
    protected Player _player;
    protected bool _isVisible = false;

    public override void _Ready()
    {
        // Try common label names used by different modals
        _itemsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/ItemsLabel");
        if (_itemsLabel == null)
        {
            _itemsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/InventoryLabel");
        }

        // Start hidden
        Hide();
    }

    /// <summary>
    /// Connects to the player to access inventory.
    /// </summary>
    public virtual void ConnectToPlayer(Player player)
    {
        _player = player;
    }

    /// <summary>
    /// Shows the menu.
    /// </summary>
    public virtual void ShowMenu()
    {
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the menu.
    /// </summary>
    public virtual void HideMenu()
    {
        _isVisible = false;
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        // Only process input when visible
        if (!_isVisible)
        {
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            HandleKeyInput(keyEvent);
        }
    }

    /// <summary>
    /// Handles key input. Can be overridden for custom input handling.
    /// </summary>
    protected virtual void HandleKeyInput(InputEventKey keyEvent)
    {
        // Cancel on ESC
        if (keyEvent.Keycode == Key.Escape)
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for a-z key selection
        if (keyEvent.Keycode >= Key.A && keyEvent.Keycode <= Key.Z)
        {
            char selectedKey = (char)('a' + (keyEvent.Keycode - Key.A));
            EmitSignal(SignalName.ItemSelected, selectedKey);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Updates the display. Must be implemented by subclasses.
    /// </summary>
    protected abstract void UpdateDisplay();

    /// <summary>
    /// Helper method to display empty inventory message.
    /// </summary>
    protected void ShowEmptyMessage(string title)
    {
        if (_itemsLabel != null)
        {
            _itemsLabel.Text = $"[center][b]{title}[/b][/center]\n[center](ESC to cancel)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No items[/color][/center]";
        }
    }

    /// <summary>
    /// Helper method to build the header for item lists.
    /// </summary>
    protected string BuildHeader(string title)
    {
        return $"[center][b]{title}[/b][/center]\n[center](ESC to cancel)[/center]\n";
    }
}
