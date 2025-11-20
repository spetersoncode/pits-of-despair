using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;

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
        // Cancel on modal close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for letter key selection
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            // Key case is now handled correctly by KeybindingService:
            // lowercase (a-z) when no Shift, uppercase (A-Z) with Shift
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
            var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
            _itemsLabel.Text = $"[center][b]{title}[/b][/center]\n[center]({closeKey} to cancel)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No items[/color][/center]";
        }
    }

    /// <summary>
    /// Helper method to build the header for item lists.
    /// </summary>
    protected string BuildHeader(string title)
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        return $"[center][b]{title}[/b][/center]\n[center]({closeKey} to cancel)[/center]\n";
    }
}
