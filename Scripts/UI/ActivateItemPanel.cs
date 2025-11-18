using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using System.Linq;
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
        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");

        if (inventory.Count == 0)
        {
            _itemsLabel.Text = $"[center][b]Activate which item?[/b][/center]\n[center](ESC to cancel)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No items[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Activate which item?[/b][/center]");
        sb.AppendLine("[center](ESC to cancel)[/center]\n");

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            // Check if item is equipped
            bool isEquipped = equipComponent != null && equipComponent.IsEquipped(slot.Key);

            // Check if item is activatable
            // - Consumables and charged items: must not be equipped
            // - Reach weapons (melee with range > 1): can be equipped
            bool isActivatable;
            if (isEquipped)
            {
                // Equipped items can only be activated if they're reach weapons
                isActivatable = slot.Item.Template.Attack != null &&
                               slot.Item.Template.Attack.Type == AttackType.Melee &&
                               slot.Item.Template.Attack.Range > 1;
            }
            else
            {
                // Non-equipped items: activatable if consumable or have charges
                isActivatable = slot.Item.Template.IsActivatable() &&
                               (slot.Item.Template.GetIsConsumable() || slot.Item.CurrentCharges > 0);
            }

            // Format: key) glyph name (count/charges) (equipped)
            string colorHex = isActivatable ? slot.Item.Template.Color : Palette.ToHex(Palette.Disabled);
            string keyColor = isActivatable ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Basalt);
            string displayName = slot.Item.Template.GetDisplayName(slot.Count);
            string chargesText = slot.Item.Template.GetMaxCharges() > 0 ? $" [{slot.Item.CurrentCharges}/{slot.Item.Template.GetMaxCharges()}]" : "";
            string equippedText = isEquipped ? $" [color={Palette.ToHex(Palette.Disabled)}](equipped)[/color]" : "";

            sb.AppendLine($"[color={keyColor}]{slot.Key})[/color] [color={colorHex}]{slot.Item.Template.GetGlyph()}[/color] [color={colorHex}]{displayName}{chargesText}[/color]{equippedText}");
        }

        _itemsLabel.Text = sb.ToString();
    }
}
