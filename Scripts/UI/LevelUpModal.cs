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
	private readonly KeybindingService _keybindingService = new();

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/ContentLabel");

		// Start hidden
		Hide();
	}

	/// <summary>
	/// Shows the level-up modal for the specified level.
	/// </summary>
	/// <param name="level">The new level the player has reached</param>
	public void ShowForLevel(int level)
	{
		_newLevel = level;
		UpdateContent();
		Show();
	}

	/// <summary>
	/// Hides the level-up modal.
	/// </summary>
	public void HideModal()
	{
		Hide();
	}

	/// <summary>
	/// Updates the modal content with current level information.
	/// </summary>
	private void UpdateContent()
	{
		_contentLabel.Text = $@"[center][color={Palette.ToHex(Palette.Player)}]╔══════════════════════════════╗
║         LEVEL UP!            ║
╚══════════════════════════════╝[/color][/center]

[center][color={Palette.ToHex(Palette.Success)}]You reached level {_newLevel}![/color]
[color={Palette.ToHex(Palette.Success)}]+4 HP gained permanently[/color][/center]

[center][color={Palette.ToHex(Palette.Alert)}]Choose a stat to increase:[/color][/center]

  [color={Palette.ToHex(Palette.Default)}][S] Strength[/color]  - Melee damage and accuracy
  [color={Palette.ToHex(Palette.Default)}][A] Agility[/color]   - Ranged damage and accuracy
  [color={Palette.ToHex(Palette.Default)}][E] Endurance[/color] - Maximum hit points
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
