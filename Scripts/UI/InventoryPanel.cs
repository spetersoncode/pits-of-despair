using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays the player's inventory as a full-screen overlay.
/// Shows items assigned to keys a-z with glyph, name, and count.
/// </summary>
public partial class InventoryPanel : PanelContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    private RichTextLabel _inventoryLabel;
    private Player _player;
    private bool _isVisible = false;

    public override void _Ready()
    {
        _inventoryLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/InventoryLabel");

        // Start hidden
        Hide();
    }

    /// <summary>
    /// Connects to the player to receive inventory updates.
    /// </summary>
    public void ConnectToPlayer(Player player)
    {
        // Unsubscribe from old player if exists
        if (_player != null)
        {
            _player.InventoryChanged -= OnInventoryChanged;
        }

        _player = player;

        // Subscribe to new player's inventory changes
        if (_player != null)
        {
            _player.InventoryChanged += OnInventoryChanged;
        }

        UpdateInventoryDisplay();
    }

    /// <summary>
    /// Toggles the inventory panel visibility.
    /// </summary>
    public void ToggleInventory()
    {
        _isVisible = !_isVisible;

        if (_isVisible)
        {
            Show();
            UpdateInventoryDisplay();
        }
        else
        {
            Hide();
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Only process input when visible
        if (!_isVisible)
        {
            return;
        }

        // Close on 'I' or ESC
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.I || keyEvent.Keycode == Key.Escape)
            {
                EmitSignal(SignalName.Cancelled);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnInventoryChanged()
    {
        UpdateInventoryDisplay();
    }

    private void UpdateInventoryDisplay()
    {
        if (_inventoryLabel == null || _player == null)
        {
            return;
        }

        var inventory = _player.Inventory;

        if (inventory.Count == 0)
        {
            _inventoryLabel.Text = "[center][b]Inventory (a-z)[/b][/center]\n\n[center][color=#888888]Empty[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Inventory (a-z)[/b][/center]\n");

        foreach (var slot in inventory)
        {
            // Format: key) glyph name (count/charges)
            string colorHex = slot.Item.Template.Color;
            string countText = slot.Count > 1 ? $" ({slot.Count})" : "";
            string chargesText = slot.Item.Template.MaxCharges > 0 ? $" [{slot.Item.CurrentCharges}/{slot.Item.Template.MaxCharges}]" : "";

            sb.AppendLine($"[color=#888888]{slot.Key})[/color] [color={colorHex}]{slot.Item.Template.Glyph}[/color] {slot.Item.Template.Name}{countText}{chargesText}");
        }

        _inventoryLabel.Text = sb.ToString();
    }
}
