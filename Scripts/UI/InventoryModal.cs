using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays the player's inventory as a full-screen overlay.
/// Shows items assigned to keys a-z with glyph, name, and count.
/// </summary>
public partial class InventoryModal : ItemSelectionModal
{
    public override void ConnectToPlayer(Player player)
    {
        // Disconnect from old player if exists
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.InventoryChanged, Callable.From(OnInventoryChanged));
        }

        base.ConnectToPlayer(player);

        // Connect to new player's inventory changes
        if (_player != null)
        {
            _player.Connect(Player.SignalName.InventoryChanged, Callable.From(OnInventoryChanged));
        }

        UpdateDisplay();
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
            UpdateDisplay();
        }
        else
        {
            Hide();
        }
    }

    protected override void HandleKeyInput(InputEventKey keyEvent)
    {
        // Close on 'I' or ESC
        if (keyEvent.Keycode == Key.I || keyEvent.Keycode == Key.Escape)
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for a-z key selection to open item details
        if (keyEvent.Keycode >= Key.A && keyEvent.Keycode <= Key.Z)
        {
            char selectedKey = (char)('a' + (keyEvent.Keycode - Key.A));

            // Check if this item exists in inventory
            var slot = _player.GetInventorySlot(selectedKey);
            if (slot != null)
            {
                EmitSignal(SignalName.ItemSelected, selectedKey);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnInventoryChanged()
    {
        UpdateDisplay();
    }

    protected override void UpdateDisplay()
    {
        if (_itemsLabel == null || _player == null)
        {
            return;
        }

        var inventory = _player.Inventory;

        if (inventory.Count == 0)
        {
            _itemsLabel.Text = $"[center][b]Inventory (a-z)[/b][/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]Empty[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Inventory (a-z)[/b][/center]\n");

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            var options = ItemDisplayOptions.ShowCount | ItemDisplayOptions.ShowSlot | ItemDisplayOptions.ShowEquipped;
            string line = ItemFormatter.FormatItemLine(slot, options, equipComponent);
            sb.AppendLine(line);
        }

        _itemsLabel.Text = sb.ToString();
    }
}
