using Godot;
using PitsOfDespair.Core;

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
        var content = new System.Text.StringBuilder();

        content.AppendLine($"[center][color={Palette.ToHex(Palette.Player)}]═══ PITS OF DESPAIR - HOW TO PLAY ═══[/color][/center]");
        content.AppendLine();

        // Movement
        content.AppendLine($"[color={Palette.ToHex(Palette.Success)}]MOVEMENT[/color]");
        content.AppendLine("  Arrow Keys / Numpad / hjklyubn - Move & attack");
        content.AppendLine("  Space / Numpad 5 / Period (.) - Wait one turn");
        content.AppendLine();

        // Combat
        content.AppendLine($"[color={Palette.ToHex(Palette.Danger)}]COMBAT[/color]");
        content.AppendLine("  Move into enemy - Melee attack");
        content.AppendLine("  f - Fire ranged weapon (if equipped)");
        content.AppendLine("    Tab / Numpad +/- - Cycle targets");
        content.AppendLine("    Arrow Keys / Numpad - Move cursor");
        content.AppendLine("    Enter / Space / f - Confirm target");
        content.AppendLine("    Escape - Cancel targeting");
        content.AppendLine();

        // Inventory & Items
        content.AppendLine($"[color={Palette.ToHex(Palette.Gold)}]INVENTORY & ITEMS[/color]");
        content.AppendLine("  i - View inventory");
        content.AppendLine("  g - Pick up item");
        content.AppendLine("  d - Drop item");
        content.AppendLine("  a - Activate/use item");
        content.AppendLine("  e - Equip/unequip item");
        content.AppendLine();

        // Exploration
        content.AppendLine($"[color={Palette.ToHex(Palette.Alert)}]EXPLORATION[/color]");
        content.AppendLine("  x - Examine mode (look around)");
        content.AppendLine("    Arrow Keys / Numpad - Move cursor");
        content.AppendLine("    x / Escape - Exit examine mode");
        content.AppendLine("  ? - Show this help screen");
        content.AppendLine("  Escape - Close menus");
        content.AppendLine();

        // Debug
        content.AppendLine($"[color={Palette.ToHex(Palette.Caution)}]DEBUG[/color]");
        content.AppendLine("  Ctrl+D - Toggle debug mode");
        content.AppendLine("  / - Open debug console (type /help for commands)");
        content.AppendLine();

        // Stats
        content.AppendLine($"[color={Palette.ToHex(Palette.Cyan)}]STATS[/color]");
        content.AppendLine("  Strength - Increases melee attack accuracy and damage");
        content.AppendLine("  Agility - Increases ranged attack accuracy and evasion");
        content.AppendLine("  Endurance - Increases maximum hit points");
        content.AppendLine("  Will - Reserved for future magic abilities");
        content.AppendLine("  Armor - Reduces damage taken from attacks");
        content.AppendLine("  Evasion - Your ability to dodge attacks (heavy armor reduces this)");
        content.AppendLine();

        content.AppendLine($"[center][color={Palette.ToHex(Palette.Disabled)}]Press Escape or ? to close[/color][/center]");

        _helpLabel.Text = content.ToString();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        // Close on Escape key
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                HideHelp();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
