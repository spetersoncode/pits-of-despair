using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays the player's inventory as a full-screen overlay.
/// Shows items assigned to keys a-z with glyph, name, and count.
/// Supports direct key rebinding via '=' key.
/// </summary>
public partial class InventoryModal : ItemSelectionModal
{
    [Signal]
    public delegate void ItemKeyReboundEventHandler(char oldKey, char newKey);

    private enum State
    {
        Viewing,
        AwaitingRebindSource,
        AwaitingRebindTarget
    }

    private State _currentState = State.Viewing;
    private char _rebindSourceKey;
    private string _rebindItemName;

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
        switch (_currentState)
        {
            case State.Viewing:
                HandleViewingInput(keyEvent);
                break;
            case State.AwaitingRebindSource:
                HandleRebindSourceInput(keyEvent);
                break;
            case State.AwaitingRebindTarget:
                HandleRebindTargetInput(keyEvent);
                break;
        }
    }

    private void HandleViewingInput(InputEventKey keyEvent)
    {
        // Close on modal close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Enter rebind mode on '='
        if (MenuInputProcessor.IsKey(keyEvent, Key.Equal))
        {
            if (_player.Inventory.Count > 0)
            {
                _currentState = State.AwaitingRebindSource;
                _rebindSourceKey = '\0';
                UpdateDisplay();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for letter key selection to open item details
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            var slot = _player.GetInventorySlot(selectedKey);
            if (slot != null)
            {
                EmitSignal(SignalName.ItemSelected, selectedKey);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void HandleRebindSourceInput(InputEventKey keyEvent)
    {
        // Cancel rebind on close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            _currentState = State.Viewing;
            UpdateDisplay();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Select item to rebind
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            var slot = _player.GetInventorySlot(selectedKey);
            if (slot != null)
            {
                _rebindSourceKey = selectedKey;
                _rebindItemName = slot.Item.Template.Name;
                _currentState = State.AwaitingRebindTarget;
                UpdateDisplay();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleRebindTargetInput(InputEventKey keyEvent)
    {
        // Cancel rebind on close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            _currentState = State.Viewing;
            UpdateDisplay();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Select target key
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char newKey))
        {
            EmitSignal(SignalName.ItemKeyRebound, _rebindSourceKey, newKey);
            _currentState = State.Viewing;
            UpdateDisplay();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void HideMenu()
    {
        _currentState = State.Viewing;
        base.HideMenu();
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
            var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
            _itemsLabel.Text = $"[center][b]INVENTORY[/b][/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]Empty[/color][/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]({closeKey} to close)[/color][/center]";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(BuildHeader());
        sb.AppendLine();

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
        var skillComponent = _player.GetNodeOrNull<SkillComponent>("SkillComponent");
        bool hasAttunement = skillComponent?.HasSkill("attunement") ?? false;

        foreach (var slot in inventory.OrderBy(s => s.Key))
        {
            var options = ItemDisplayOptions.ShowCount | ItemDisplayOptions.ShowSlot | ItemDisplayOptions.ShowEquipped;
            if (hasAttunement)
            {
                options |= ItemDisplayOptions.ShowChargeBrackets;
            }
            string line = ItemFormatter.FormatItemLine(slot, options, equipComponent);
            sb.AppendLine(line);
        }

        _itemsLabel.Text = sb.ToString();
    }

    private string BuildHeader()
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);

        switch (_currentState)
        {
            case State.AwaitingRebindSource:
                return $"[center][b][color={Palette.ToHex(Palette.Caution)}]REBIND MODE[/color][/b][/center]\n[center]Press a-z to select item to rebind[/center]\n[center][color={Palette.ToHex(Palette.Disabled)}]({closeKey} to cancel)[/color][/center]";

            case State.AwaitingRebindTarget:
                return $"[center][b][color={Palette.ToHex(Palette.Caution)}]REBIND MODE[/color][/b][/center]\n[center]Rebinding [color={Palette.ToHex(Palette.Default)}]{_rebindItemName}[/color] from '{_rebindSourceKey}'[/center]\n[center]Press a-z to assign new key[/center]\n[center][color={Palette.ToHex(Palette.Disabled)}]({closeKey} to cancel)[/color][/center]";

            default:
                return $"[center][b]INVENTORY[/b][/center]\n[center][color={Palette.ToHex(Palette.Disabled)}][=] Rebind   ({closeKey} to close)[/color][/center]";
        }
    }
}
