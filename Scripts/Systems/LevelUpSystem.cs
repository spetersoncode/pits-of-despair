using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages level-up rewards and stat increases for the player.
/// Listens to StatsComponent.LevelUp signal and:
/// - Coordinates stat choice UI
/// - Applies chosen stat increases
/// Note: HP gains come purely from Endurance stat investment (no automatic HP per level).
/// </summary>
public partial class LevelUpSystem : Node
{
	#region Signals

	/// <summary>
	/// Emitted when the player levels up and should be shown the stat choice modal.
	/// Parameter: new level
	/// </summary>
	[Signal]
	public delegate void ShowLevelUpModalEventHandler(int newLevel);

	/// <summary>
	/// Emitted after a stat has been increased from level-up choice.
	/// Parameters: stat name, new level
	/// </summary>
	[Signal]
	public delegate void StatIncreasedEventHandler(string statName, int newLevel);

	#endregion

	#region State

	private Player _player;
	private StatsComponent _statsComponent;
	private int _pendingLevel; // Level waiting for stat choice

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the level-up system with the player entity.
	/// </summary>
	/// <param name="player">The player entity</param>
	public void Initialize(Player player)
	{
		if (player == null)
		{
			GD.PushError("LevelUpSystem: Cannot initialize with null player.");
			return;
		}

		_player = player;
		_statsComponent = _player.GetNodeOrNull<StatsComponent>("StatsComponent");

		if (_statsComponent == null)
		{
			GD.PushError("LevelUpSystem: Player missing StatsComponent.");
			return;
		}

		// Listen to level-up signal from StatsComponent
		_statsComponent.Connect(
			StatsComponent.SignalName.LevelUp,
			Callable.From<int>(OnPlayerLevelUp)
		);
	}

	#endregion

	#region Level-Up Handling

	/// <summary>
	/// Called when the player levels up.
	/// Requests stat choice from UI. HP gains now come purely from END stat investment.
	/// </summary>
	/// <param name="newLevel">The new level reached</param>
	private void OnPlayerLevelUp(int newLevel)
	{
		_pendingLevel = newLevel;

		// Request UI to show stat choice modal
		// Note: No automatic HP increase - all HP gains come from END stat choices
		EmitSignal(SignalName.ShowLevelUpModal, newLevel);
	}

	#endregion

	#region Stat Choice Application

	/// <summary>
	/// Applies the player's stat choice from level-up.
	/// </summary>
	/// <param name="statIndex">Index of the chosen stat (0=STR, 1=AGI, 2=END, 3=WIL)</param>
	public void ApplyStatChoice(int statIndex)
	{
		if (_statsComponent == null)
		{
			GD.PushError("LevelUpSystem: Cannot apply stat choice, StatsComponent is null.");
			return;
		}

		string statName = statIndex switch
		{
			0 => ApplyStrengthIncrease(),
			1 => ApplyAgilityIncrease(),
			2 => ApplyEnduranceIncrease(),
			3 => ApplyWillIncrease(),
			_ => null
		};

		if (statName == null)
		{
			GD.PushWarning($"LevelUpSystem: Invalid stat index {statIndex}.");
			return;
		}

		// Trigger stat recalculation
		_statsComponent.EmitSignal(StatsComponent.SignalName.StatsChanged);

		// Notify that stat was increased
		EmitSignal(SignalName.StatIncreased, statName, _pendingLevel);
	}

	private string ApplyStrengthIncrease()
	{
		_statsComponent.BaseStrength++;
		return "Strength";
	}

	private string ApplyAgilityIncrease()
	{
		_statsComponent.BaseAgility++;
		return "Agility";
	}

	private string ApplyEnduranceIncrease()
	{
		_statsComponent.BaseEndurance++;
		return "Endurance";
	}

	private string ApplyWillIncrease()
	{
		_statsComponent.BaseWill++;
		return "Will";
	}

	#endregion

	#region Cleanup

	public override void _ExitTree()
	{
		// Disconnect from StatsComponent signal
		if (_statsComponent != null)
		{
			_statsComponent.Disconnect(
				StatsComponent.SignalName.LevelUp,
				Callable.From<int>(OnPlayerLevelUp)
			);
		}
	}

	#endregion
}
