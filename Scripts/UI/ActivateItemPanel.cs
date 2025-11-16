using Godot;
using PitsOfDespair.Entities;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for activating (using) items from inventory.
/// Shows items with their keys, greying out non-activatable items.
/// </summary>
public partial class ActivateItemPanel : PanelContainer
{
    [Signal]
    public delegate void ItemSelectedEventHandler(char key);

    [Signal]
    public delegate void CancelledEventHandler();

    private RichTextLabel _itemsLabel;
    private Player _player;
    private bool _isVisible = false;

    public override void _Ready()
    {
        _itemsLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/ItemsLabel");

        // Start hidden
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
    /// Shows the activate item menu.
    /// </summary>
    public void ShowMenu()
    {
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the activate item menu.
    /// </summary>
    public void HideMenu()
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
    }

    private void UpdateDisplay()
    {
        if (_itemsLabel == null || _player == null)
        {
            return;
        }

        var inventory = _player.Inventory;

        if (inventory.Count == 0)
        {
            _itemsLabel.Text = "[center][b]Activate which item?[/b][/center]\n[center](ESC to cancel)[/center]\n\n[center][color=#888888]No items[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Activate which item?[/b][/center]");
        sb.AppendLine("[center](ESC to cancel)[/center]\n");

        foreach (var slot in inventory)
        {
            // Check if item is activatable
            bool isActivatable = slot.ItemData.IsActivatable;

            // Format: key) glyph name (count)
            string colorHex = isActivatable ? slot.ItemData.Color : "#888888";
            string keyColor = isActivatable ? "#888888" : "#444444";
            string countText = slot.Count > 1 ? $" ({slot.Count})" : "";

            sb.AppendLine($"[color={keyColor}]{slot.Key})[/color] [color={colorHex}]{slot.ItemData.Glyph}[/color] [color={colorHex}]{slot.ItemData.Name}{countText}[/color]");
        }

        _itemsLabel.Text = sb.ToString();
    }
}
