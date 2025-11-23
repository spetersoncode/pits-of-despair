using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages level-up rewards and stat increases for the player.
/// Listens to StatsComponent.LevelUp signal and:
/// - Coordinates stat choice UI
/// - Applies chosen stat increases
/// - Queues multiple level-ups for discrete processing
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

	/// <summary>
	/// Emitted when the pending level-up count changes (for HUD indicator).
	/// Parameter: count of pending level-ups
	/// </summary>
	[Signal]
	public delegate void PendingLevelUpsChangedEventHandler(int count);

	#endregion

	#region State

	private Player _player;
	private StatsComponent _statsComponent;
	private readonly Queue<int> _pendingLevelUps = new(); // Queue of levels waiting for processing
	private bool _isProcessingLevelUp = false; // Prevent overlapping modals

	/// <summary>
	/// Whether there are pending level-ups waiting to be processed.
	/// </summary>
	public bool HasPendingLevelUp => _pendingLevelUps.Count > 0;

	/// <summary>
	/// Number of pending level-ups waiting to be processed.
	/// </summary>
	public int PendingLevelUpCount => _pendingLevelUps.Count;

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
	/// Queues the level-up for later processing when player presses L key.
	/// </summary>
	/// <param name="newLevel">The new level reached</param>
	private void OnPlayerLevelUp(int newLevel)
	{
		// Queue this level-up
		_pendingLevelUps.Enqueue(newLevel);
		EmitSignal(SignalName.PendingLevelUpsChanged, _pendingLevelUps.Count);

		// Don't auto-open modal - player will see "LEVEL UP! [L]" indicator
		// and can press L when ready to make their choices
	}

	/// <summary>
	/// Processes the next pending level-up by showing the modal.
	/// </summary>
	private void ProcessNextLevelUp()
	{
		if (_pendingLevelUps.Count == 0)
		{
			_isProcessingLevelUp = false;
			return;
		}

		_isProcessingLevelUp = true;
		int nextLevel = _pendingLevelUps.Peek(); // Don't dequeue yet - wait for completion

		// Request UI to show stat choice modal
		EmitSignal(SignalName.ShowLevelUpModal, nextLevel);
	}

	/// <summary>
	/// Manually triggers the level-up modal if there are pending level-ups.
	/// Called when player presses L key.
	/// </summary>
	public void TriggerPendingLevelUp()
	{
		if (HasPendingLevelUp && !_isProcessingLevelUp)
		{
			ProcessNextLevelUp();
		}
	}

	/// <summary>
	/// Called when a level-up has been fully completed (stat chosen, skill chosen if applicable).
	/// Advances to the next pending level-up if any.
	/// </summary>
	public void CompleteLevelUp()
	{
		if (_pendingLevelUps.Count > 0)
		{
			_pendingLevelUps.Dequeue();
			EmitSignal(SignalName.PendingLevelUpsChanged, _pendingLevelUps.Count);
		}

		_isProcessingLevelUp = false;

		// Note: We don't auto-process next level-up here
		// Player must press L or trigger will happen naturally on next XP gain
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

		// Get the level being processed (peeked but not yet dequeued)
		int currentLevel = _pendingLevelUps.Count > 0 ? _pendingLevelUps.Peek() : _statsComponent.Level;

		// Notify that stat was increased
		EmitSignal(SignalName.StatIncreased, statName, currentLevel);
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
