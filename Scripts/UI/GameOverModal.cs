using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.UI;

/// <summary>
/// Full-screen game over modal displayed when the player dies.
/// Shows game over image, player name, floor reached, and prompts to try again.
/// </summary>
public partial class GameOverModal : Control
{
	[Signal]
	public delegate void RestartRequestedEventHandler();

	private TextureRect _gameOverImage;
	private RichTextLabel _deathInfoLabel;

	private string _playerName = "Player";
	private int _floorReached = 1;

	public override void _Ready()
	{
		_gameOverImage = GetNode<TextureRect>("GameOverImage");
		_deathInfoLabel = GetNode<RichTextLabel>("DeathInfoLabel");

		// Start hidden
		Hide();
	}

	/// <summary>
	/// Shows the game over modal with death information.
	/// </summary>
	public void ShowGameOver(string playerName, int floorReached)
	{
		_playerName = playerName;
		_floorReached = floorReached;

		UpdateDeathInfo();
		Show();
	}

	/// <summary>
	/// Hides the game over modal.
	/// </summary>
	public void HideGameOver()
	{
		Hide();
	}

	/// <summary>
	/// Updates the death info text.
	/// </summary>
	private void UpdateDeathInfo()
	{
		string nameColor = Palette.ToHex(Palette.Player);
		string floorColor = Palette.ToHex(Palette.Alert);

		_deathInfoLabel.Text = $"[center][color={nameColor}]{_playerName}[/color] perished on floor [color={floorColor}]{_floorReached}[/color][/center]";
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			// Check for N key to restart
			if (keyEvent.Keycode == Key.N)
			{
				EmitSignal(SignalName.RestartRequested);
				GetViewport().SetInputAsHandled();
			}
		}
	}
}
