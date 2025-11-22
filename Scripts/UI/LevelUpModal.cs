using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Services;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal displayed when the player levels up.
/// Allows the player to choose one stat to increase permanently.
/// Cannot be cancelled - player must make a choice.
/// </summary>
public partial class LevelUpModal : PanelContainer
{
    [Signal]
    public delegate void StatChosenEventHandler(int statIndex);

    private RichTextLabel _contentLabel;
    private int _newLevel;
    private readonly KeybindingService _keybindingService = KeybindingService.Instance;

    public override void _Ready()
    {
        _contentLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/ContentLabel");

        // Start hidden
        Hide();
    }

    private int _currentEndurance;

    /// <summary>
    /// Shows the level-up modal for the specified level.
    /// </summary>
    /// <param name="level">The new level the player has reached</param>
    /// <param name="currentEndurance">The player's current endurance stat</param>
    public void ShowForLevel(int level, int currentEndurance)
    {
        _newLevel = level;
        _currentEndurance = currentEndurance;
        UpdateContent();

        // Switch to Modal input context so stat selection keys take priority
        _keybindingService.CurrentContext = InputContext.Modal;

        Show();
    }

    /// <summary>
    /// Hides the level-up modal.
    /// </summary>
    public void HideModal()
    {
        // Restore Gameplay input context
        _keybindingService.CurrentContext = InputContext.Gameplay;

        Hide();
    }

    /// <summary>
    /// Updates the modal content with current level information.
    /// </summary>
    private void UpdateContent()
    {
        // Calculate HP gain for next point of endurance
        // Formula is (END^2 + 9*END)/2, so marginal gain is 4 + END
        // If we add 1 to END, the new gain is 4 + (END+1) = 5 + END
        // Wait, let's check the math in StatsComponent.cs
        // GetHPBonus: (endurance * endurance + 9 * endurance) / 2
        // Gain from END to END+1:
        // ((E+1)^2 + 9(E+1))/2 - (E^2 + 9E)/2
        // (E^2 + 2E + 1 + 9E + 9 - E^2 - 9E) / 2
        // (2E + 10) / 2 = E + 5
        // So if current endurance is _currentEndurance, adding 1 gives (_currentEndurance + 1) + 4 = _currentEndurance + 5 HP.
        // Example: END=0 -> Gain=5. END=1 -> Gain=6.

        int hpGain = _currentEndurance + 5;

        _contentLabel.Text = $@"[center][color={Palette.ToHex(Palette.Player)}]╔══════════════════════════════╗
║         LEVEL UP!            ║
╚══════════════════════════════╝[/color][/center]

[center][color={Palette.ToHex(Palette.Success)}]You reached level {_newLevel}![/color][/center]

[center][color={Palette.ToHex(Palette.Alert)}]Choose a stat to increase:[/color][/center]

  [color={Palette.ToHex(Palette.Default)}][S] Strength[/color]  - Melee damage and accuracy
  [color={Palette.ToHex(Palette.Default)}][A] Agility[/color]   - Ranged damage and accuracy
  [color={Palette.ToHex(Palette.Default)}][E] Endurance[/color] - +{hpGain} hit points (gain increases every level)
  [color={Palette.ToHex(Palette.Default)}][W] Will[/color]      - Reserved for future magic";
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Check for stat selection actions using the keybinding service
            if (_keybindingService.TryGetAction(keyEvent, out var action))
            {
                int statIndex = -1;

                // Map action to stat index
                switch (action)
                {
                    case InputAction.SelectStrength:
                        statIndex = 0;
                        break;
                    case InputAction.SelectAgility:
                        statIndex = 1;
                        break;
                    case InputAction.SelectEndurance:
                        statIndex = 2;
                        break;
                    case InputAction.SelectWill:
                        statIndex = 3;
                        break;
                }

                if (statIndex >= 0)
                {
                    EmitSignal(SignalName.StatChosen, statIndex);
                    GetViewport().SetInputAsHandled();
                }
            }

            // Note: ESC does NOT close this modal - player must choose
        }
    }
}
