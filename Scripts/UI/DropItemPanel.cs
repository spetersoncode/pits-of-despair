using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for dropping items from inventory.
/// Shows all items with their keys for selection.
/// </summary>
public partial class DropItemPanel : PanelContainer
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
    /// Shows the drop item menu.
    /// </summary>
    public void ShowMenu()
    {
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the drop item menu.
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
        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

        if (inventory.Count == 0)
        {
            _itemsLabel.Text = "[center][b]Drop which item?[/b][/center]\n[center](ESC to cancel)[/center]\n\n[center][color=#888888]No items[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Drop which item?[/b][/center]");
        sb.AppendLine("[center](ESC to cancel)[/center]\n");

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            // Check if item is equipped
            bool isEquipped = equipComponent != null && equipComponent.IsEquipped(slot.Key);

            // Format: key) glyph name (count/charges) (equipped)
            string colorHex = slot.Item.Template.Color;
            string countText = slot.Count > 1 ? $" ({slot.Count})" : "";
            string chargesText = slot.Item.Template.GetMaxCharges() > 0 ? $" [{slot.Item.CurrentCharges}/{slot.Item.Template.GetMaxCharges()}]" : "";
            string equippedText = isEquipped ? " [color=#888888](equipped)[/color]" : "";

            sb.AppendLine($"[color=#888888]{slot.Key})[/color] [color={colorHex}]{slot.Item.Template.GetGlyph()}[/color] {slot.Item.Template.Name}{countText}{chargesText}{equippedText}");
        }

        _itemsLabel.Text = sb.ToString();
    }
}
