using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a selection menu for equipping items from inventory.
/// Shows items with their keys, greying out non-equippable items.
/// </summary>
public partial class EquipPanel : PanelContainer
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
    /// Shows the equip menu.
    /// </summary>
    public void ShowMenu()
    {
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the equip menu.
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
            _itemsLabel.Text = $"[center][b]Equip which item?[/b][/center]\n[center](ESC to cancel)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No items[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[center][b]Equip which item?[/b][/center]");
        sb.AppendLine("[center](ESC to cancel)[/center]\n");

        foreach (var slot in inventory)
        {
            var itemTemplate = slot.Item.Template;
            bool isEquippable = itemTemplate.GetIsEquippable();
            var equipSlot = itemTemplate.GetEquipmentSlot();

            // Check if this item is currently equipped
            bool isEquipped = equipComponent?.IsEquipped(slot.Key) ?? false;
            string equippedText = "";
            if (isEquipped)
            {
                var currentSlot = equipComponent.GetSlotForItem(slot.Key);
                equippedText = $" [color={Palette.ToHex(Palette.Success)}]{{EQUIPPED: {FormatSlotName(currentSlot)}}}[/color]";
            }

            // Format: key) glyph name [slot] {EQUIPPED: SlotName}
            string colorHex = isEquippable ? itemTemplate.Color : Palette.ToHex(Palette.Basalt);
            string keyColor = isEquippable ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Basalt);
            string slotText = isEquippable ? $" [color={Palette.ToHex(Palette.AshGray)}][{FormatSlotName(equipSlot)}][/color]" : "";
            string countText = slot.Count > 1 ? $" ({slot.Count})" : "";

            sb.AppendLine($"[color={keyColor}]{slot.Key})[/color] [color={colorHex}]{itemTemplate.GetGlyph()}[/color] [color={colorHex}]{itemTemplate.Name}{countText}[/color]{slotText}{equippedText}");
        }

        _itemsLabel.Text = sb.ToString();
    }

    /// <summary>
    /// Formats an equipment slot name for display.
    /// </summary>
    private string FormatSlotName(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MeleeWeapon => "Melee",
            EquipmentSlot.RangedWeapon => "Ranged",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Ring1 => "Ring1",
            EquipmentSlot.Ring2 => "Ring2",
            _ => "Unknown"
        };
    }
}
