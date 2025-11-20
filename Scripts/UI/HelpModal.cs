using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays game controls and help information as a full-screen overlay.
/// Activated by pressing ? key.
/// </summary>
public partial class HelpModal : PanelContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    private RichTextLabel _helpLabel;

    public override void _Ready()
    {
        _helpLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/HelpLabel");

        // Start hidden
        Hide();

        // Populate help content
        UpdateHelpContent();
    }

    /// <summary>
    /// Shows the help modal.
    /// </summary>
    public void ShowHelp()
    {
        Show();
        _helpLabel.GrabFocus();
    }

    /// <summary>
    /// Hides the help modal.
    /// </summary>
    public void HideHelp()
    {
        Hide();
        EmitSignal(SignalName.Cancelled);
    }

    /// <summary>
    /// Populates the help content with game controls and information.
    /// </summary>
    private void UpdateHelpContent()
    {
        _helpLabel.Text = $@"[center][color={Palette.ToHex(Palette.Player)}]PITS OF DESPAIR[/color][/center]

[color={Palette.ToHex(Palette.Success)}]MOVEMENT[/color]
  Arrow Keys / Numpad - Move & attack
  Space / Numpad 5 - Wait

[color={Palette.ToHex(Palette.Alert)}]ACTIONS[/color]
  G - Pickup   F - Fire Ranged

[color={Palette.ToHex(Palette.Gold)}]MENUS[/color]
  I - Inventory   A - Activate   D - Drop
  E - Equip       X - Examine

[color={Palette.ToHex(Palette.Caution)}]SYSTEM[/color]
  ? - Help   Ctrl+D - Debug   / - Console

[color={Palette.ToHex(Palette.Danger)}]TARGETING[/color]
  Enter/Space/F - Confirm   Esc - Cancel
  Tab/Numpad +/- - Cycle targets

[color={Palette.ToHex(Palette.Cyan)}]STATS[/color]
  Strength - Melee damage and accuracy
  Agility - Ranged damage and accuracy
  Endurance - Maximum hit points
  Will - Reserved for future magic
  Armor - Reduces incoming damage
  Evasion - Chance to dodge attacks

[color={Palette.ToHex(Palette.Caution)}]DEBUG COMMANDS[/color]
  Ctrl-D - Toggle Debug Mode   / - enter command (if debug mode enabled)
  Type /help in console for all commands

[center][color={Palette.ToHex(Palette.Disabled)}]Esc to close[/color][/center]";
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        // Close on modal close key (Escape)
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (MenuInputProcessor.IsCloseKey(keyEvent))
            {
                HideHelp();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
