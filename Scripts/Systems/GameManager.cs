using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Root game coordinator that manages multi-floor dungeon progression.
/// Owns persistent state (gold, player state) and orchestrates floor transitions.
/// </summary>
public partial class GameManager : Node
{
	#region Constants

	private const int MaxFloorDepth = 10;
	private const string GameLevelScenePath = "res://Scenes/Main/GameLevel.tscn";

	#endregion

	#region Signals

	/// <summary>
	/// Emitted when the player descends to a new floor.
	/// Parameters: old floor, new floor
	/// </summary>
	[Signal]
	public delegate void FloorChangedEventHandler(int oldFloor, int newFloor);

	/// <summary>
	/// Emitted when the player wins the game.
	/// </summary>
	[Signal]
	public delegate void GameWonEventHandler();

	/// <summary>
	/// Emitted when the player dies.
	/// Parameter: floor reached
	/// </summary>
	[Signal]
	public delegate void GameLostEventHandler(int floorReached);

	#endregion

	#region Properties

	/// <summary>
	/// Current floor depth (1-10).
	/// </summary>
	public int CurrentFloorDepth { get; private set; } = 1;

	/// <summary>
	/// Persistent gold manager shared across all floors.
	/// </summary>
	private GoldManager _goldManager;

	/// <summary>
	/// Persistent debug mode state across floors.
	/// </summary>
	private bool _debugModeActive = false;

	/// <summary>
	/// Saved player state for floor transitions.
	/// </summary>
	private PlayerState? _savedPlayerState = null;

	/// <summary>
	/// Current GameLevel instance.
	/// </summary>
	private GameLevel? _currentGameLevel = null;

	#endregion

	#region Initialization

	public override void _Ready()
	{
		// Create persistent GoldManager
		_goldManager = new GoldManager { Name = "GoldManager" };
		AddChild(_goldManager);

		// Start first floor
		StartFloor(1, null);
	}

	#endregion

	#region Floor Management

	/// <summary>
	/// Starts a new floor with the given depth and optional player state.
	/// </summary>
	/// <param name="floorDepth">The floor depth (1-10).</param>
	/// <param name="playerState">Optional player state to restore (null for new game).</param>
	private void StartFloor(int floorDepth, PlayerState? playerState)
	{
		CurrentFloorDepth = floorDepth;

		// Load GameLevel scene
		var gameLevelScene = GD.Load<PackedScene>(GameLevelScenePath);
		if (gameLevelScene == null)
		{
			GD.PrintErr($"GameManager: Failed to load GameLevel scene at {GameLevelScenePath}");
			return;
		}

		// Instantiate GameLevel
		_currentGameLevel = gameLevelScene.Instantiate<GameLevel>();
		if (_currentGameLevel == null)
		{
			GD.PrintErr("GameManager: Failed to instantiate GameLevel");
			return;
		}

		// Set floor depth
		_currentGameLevel.FloorDepth = floorDepth;

		// Add to scene tree
		AddChild(_currentGameLevel);

		// Store the player state for the deferred call
		_savedPlayerState = playerState;

		// Wait for GameLevel to finish _Ready() before applying state
		CallDeferred(nameof(OnGameLevelReady));
	}

	/// <summary>
	/// Called after GameLevel's _Ready() completes.
	/// Applies player state and connects to game events.
	/// </summary>
	private void OnGameLevelReady()
	{
		if (_currentGameLevel == null)
		{
			GD.PrintErr("GameManager.OnGameLevelReady: _currentGameLevel is null");
			return;
		}

		// Apply player state if restoring from previous floor
		if (_savedPlayerState != null)
		{
			_savedPlayerState.ApplyToPlayer(_currentGameLevel.Player);
			_savedPlayerState = null; // Clear after applying
		}

		// Give player reference to GoldManager
		_currentGameLevel.Player.SetGoldManager(_goldManager);

		// Connect to player death
		var healthComponent = _currentGameLevel.Player.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
		if (healthComponent != null)
		{
			healthComponent.Connect(Components.HealthComponent.SignalName.Died, Callable.From(OnPlayerDied));
		}

	}

	#endregion

	#region Floor Transitions

	/// <summary>
	/// Handles descending to the next floor.
	/// Called by DescendAction when player uses stairs.
	/// </summary>
	public void DescendToNextFloor()
	{
		if (_currentGameLevel == null)
		{
			GD.PrintErr("GameManager.DescendToNextFloor: No current game level");
			return;
		}

		if (CurrentFloorDepth >= MaxFloorDepth)
		{
			GD.PrintErr($"GameManager.DescendToNextFloor: Already at max floor depth ({MaxFloorDepth})");
			return;
		}

		// Extract player state
		_savedPlayerState = PlayerState.ExtractFromPlayer(_currentGameLevel.Player);

		// Extract debug mode state from GameHUD
		var gameHUD = _currentGameLevel.GetNodeOrNull<UI.GameHUD>("HUD/GameHUD");
		if (gameHUD != null)
		{
			var debugConsole = gameHUD.GetNodeOrNull<UI.DebugConsoleModal>("DebugConsoleModal");
			if (debugConsole != null)
			{
				_debugModeActive = debugConsole.IsDebugModeActive;
			}
		}

		// Save old floor for signal
		int oldFloor = CurrentFloorDepth;
		int newFloor = CurrentFloorDepth + 1;

		// Destroy current floor
		DestroyCurrentFloor();

		// Create next floor
		StartFloor(newFloor, _savedPlayerState);

		// Emit signal
		EmitSignal(SignalName.FloorChanged, oldFloor, newFloor);
	}

	/// <summary>
	/// Destroys the current GameLevel instance and cleans up resources.
	/// </summary>
	private void DestroyCurrentFloor()
	{
		if (_currentGameLevel != null)
		{
			// Disconnect from player health
			var healthComponent = _currentGameLevel.Player.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
			if (healthComponent != null)
			{
				if (healthComponent.IsConnected(Components.HealthComponent.SignalName.Died, Callable.From(OnPlayerDied)))
				{
					healthComponent.Disconnect(Components.HealthComponent.SignalName.Died, Callable.From(OnPlayerDied));
				}
			}

			// Remove and free GameLevel
			RemoveChild(_currentGameLevel);
			_currentGameLevel.QueueFree();
			_currentGameLevel = null;
		}
	}

	#endregion

	#region Game End Conditions

	/// <summary>
	/// Handles player reaching the Throne of Despair (win condition).
	/// </summary>
	public void OnThroneReached()
	{
		EmitSignal(SignalName.GameWon);

		// TODO: Show victory screen, restart option, etc.
	}

	/// <summary>
	/// Handles player death.
	/// </summary>
	private void OnPlayerDied()
	{
		EmitSignal(SignalName.GameLost, CurrentFloorDepth);

		// TODO: Show death screen, score, restart option, etc.
	}

	#endregion

	#region Accessors

	/// <summary>
	/// Gets the persistent gold manager.
	/// </summary>
	public GoldManager GetGoldManager()
	{
		return _goldManager;
	}

	/// <summary>
	/// Gets the current game level instance.
	/// </summary>
	public GameLevel? GetCurrentGameLevel()
	{
		return _currentGameLevel;
	}

	/// <summary>
	/// Gets the persistent debug mode state.
	/// </summary>
	public bool GetDebugModeActive()
	{
		return _debugModeActive;
	}

	#endregion
}
