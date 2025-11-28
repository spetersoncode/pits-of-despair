using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Entity;
using PitsOfDespair.UI;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Systems;

/// <summary>
/// Game states for the main game flow.
/// </summary>
public enum GameState
{
	Title,
	Playing,
	GameOver
}

/// <summary>
/// Root game coordinator that manages multi-floor dungeon progression.
/// Owns persistent state (gold, player state) and orchestrates floor transitions.
/// </summary>
public partial class GameManager : Node
{
	#region Constants

	private const int MaxFloorDepth = 10;
	private const string GameLevelScenePath = "res://Scenes/Main/GameLevel.tscn";
	private const string TitleModalScenePath = "res://Scenes/UI/Modals/TitleModal.tscn";
	private const string CharacterNameModalScenePath = "res://Scenes/UI/Modals/CharacterNameModal.tscn";
	private const string GameOverModalScenePath = "res://Scenes/UI/Modals/GameOverModal.tscn";

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
	/// Current game state (Title, Playing, GameOver).
	/// </summary>
	public GameState CurrentState { get; private set; } = GameState.Title;

	/// <summary>
	/// Current player name for this game session.
	/// </summary>
	public string CurrentPlayerName { get; private set; } = "Player";

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
	/// Saved companion states for floor transitions.
	/// </summary>
	private List<CompanionState>? _savedCompanionStates = null;

	/// <summary>
	/// Current GameLevel instance.
	/// </summary>
	private GameLevel? _currentGameLevel = null;

	/// <summary>
	/// CanvasLayer for menu UI (title, game over modals).
	/// </summary>
	private CanvasLayer _menuLayer;

	/// <summary>
	/// Title screen modal.
	/// </summary>
	private TitleModal _titleModal;

	/// <summary>
	/// Character name entry modal.
	/// </summary>
	private CharacterNameModal _characterNameModal;

	/// <summary>
	/// Game over modal.
	/// </summary>
	private GameOverModal _gameOverModal;

	#endregion

	#region Initialization

	public override void _Ready()
	{
		// Create persistent GoldManager
		_goldManager = new GoldManager { Name = "GoldManager" };
		AddChild(_goldManager);

		// Create menu layer for title/game over modals (renders on top)
		_menuLayer = new CanvasLayer { Name = "MenuLayer" };
		_menuLayer.Layer = 100; // High layer to render above game
		AddChild(_menuLayer);

		// Load and instantiate modals
		LoadMenuModals();

		// Start in Title state
		CurrentState = GameState.Title;
		_titleModal.ShowTitle();
	}

	/// <summary>
	/// Loads and sets up the menu modals (title, character name, game over).
	/// </summary>
	private void LoadMenuModals()
	{
		// Load Title Modal
		var titleScene = GD.Load<PackedScene>(TitleModalScenePath);
		if (titleScene != null)
		{
			_titleModal = titleScene.Instantiate<TitleModal>();
			_menuLayer.AddChild(_titleModal);
			_titleModal.Connect(TitleModal.SignalName.NewGameRequested, Callable.From(OnNewGameRequested));
		}
		else
		{
			GD.PrintErr($"GameManager: Failed to load TitleModal at {TitleModalScenePath}");
		}

		// Load Character Name Modal
		var nameScene = GD.Load<PackedScene>(CharacterNameModalScenePath);
		if (nameScene != null)
		{
			_characterNameModal = nameScene.Instantiate<CharacterNameModal>();
			_menuLayer.AddChild(_characterNameModal);
			_characterNameModal.Connect(CharacterNameModal.SignalName.NameEntered, Callable.From<string>(OnNameEntered));
			_characterNameModal.Connect(CharacterNameModal.SignalName.Cancelled, Callable.From(OnNameEntryCancelled));
		}
		else
		{
			GD.PrintErr($"GameManager: Failed to load CharacterNameModal at {CharacterNameModalScenePath}");
		}

		// Load Game Over Modal
		var gameOverScene = GD.Load<PackedScene>(GameOverModalScenePath);
		if (gameOverScene != null)
		{
			_gameOverModal = gameOverScene.Instantiate<GameOverModal>();
			_menuLayer.AddChild(_gameOverModal);
			_gameOverModal.Connect(GameOverModal.SignalName.RestartRequested, Callable.From(OnRestartRequested));
		}
		else
		{
			GD.PrintErr($"GameManager: Failed to load GameOverModal at {GameOverModalScenePath}");
		}
	}

	#endregion

	#region Menu Flow Handlers

	/// <summary>
	/// Called when player presses N on title screen.
	/// Shows the character name entry modal.
	/// </summary>
	private void OnNewGameRequested()
	{
		_titleModal.HideTitle();
		_characterNameModal.ShowNameEntry();
	}

	/// <summary>
	/// Called when player enters their character name.
	/// Starts a new game with the given name.
	/// </summary>
	private void OnNameEntered(string name)
	{
		CurrentPlayerName = name;
		_characterNameModal.HideNameEntry();
		StartNewGame();
	}

	/// <summary>
	/// Called when player cancels name entry.
	/// Returns to title screen.
	/// </summary>
	private void OnNameEntryCancelled()
	{
		_characterNameModal.HideNameEntry();
		_titleModal.ShowTitle();
	}

	/// <summary>
	/// Called when player requests restart from game over screen.
	/// Returns to title screen.
	/// </summary>
	private void OnRestartRequested()
	{
		_gameOverModal.HideGameOver();
		ReturnToTitle();
	}

	/// <summary>
	/// Starts a new game with the current player name.
	/// </summary>
	private void StartNewGame()
	{
		// Reset game state
		ResetGameState();

		// Transition to playing
		CurrentState = GameState.Playing;

		// Start first floor
		StartFloor(1, null);
	}

	/// <summary>
	/// Resets all game state for a new game.
	/// </summary>
	private void ResetGameState()
	{
		_goldManager.ResetGold();
		_savedPlayerState = null;
		_savedCompanionStates = null;
		CurrentFloorDepth = 1;
		_debugModeActive = false;
	}

	/// <summary>
	/// Returns to the title screen, cleaning up the current game.
	/// </summary>
	private void ReturnToTitle()
	{
		// Destroy current floor if exists
		DestroyCurrentFloor();

		// Transition to title state
		CurrentState = GameState.Title;
		_titleModal.ShowTitle();
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
		// Note: Player name is set in GameLevel._Ready() from GameManager.CurrentPlayerName

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
	/// <returns>Tuple of (success, message) for the action result.</returns>
	public (bool Success, string Message) DescendToNextFloor()
	{
		if (_currentGameLevel == null)
		{
			GD.PrintErr("GameManager.DescendToNextFloor: No current game level");
			return (false, "Cannot descend: system error.");
		}

		if (CurrentFloorDepth >= MaxFloorDepth)
		{
			GD.PrintErr($"GameManager.DescendToNextFloor: Already at max floor depth ({MaxFloorDepth})");
			return (false, "You have reached the deepest level.");
		}

		// Temporary floor limit during development
		if (CurrentFloorDepth >= 3)
		{
			return (false, "Under construction... come back later.");
		}

		// Extract player state
		_savedPlayerState = PlayerState.ExtractFromPlayer(_currentGameLevel.Player);

		// Extract companion states (player faction entities excluding the player)
		_savedCompanionStates = ExtractCompanionStates();

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

		return (true, "You descend the stairs into darkness...");
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

		// Transition to game over state
		CurrentState = GameState.GameOver;
		_gameOverModal.ShowGameOver(CurrentPlayerName, CurrentFloorDepth);
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

	/// <summary>
	/// Gets the saved companion states for the current floor transition.
	/// Returns null if no companions are saved (e.g., first floor).
	/// </summary>
	public List<CompanionState>? GetSavedCompanionStates()
	{
		return _savedCompanionStates;
	}

	/// <summary>
	/// Clears the saved companion states after they have been restored.
	/// </summary>
	public void ClearSavedCompanionStates()
	{
		_savedCompanionStates = null;
	}

	#endregion

	#region Companion Management

	/// <summary>
	/// Extracts state from all player faction companions on the current floor.
	/// </summary>
	private List<CompanionState> ExtractCompanionStates()
	{
		var companionStates = new List<CompanionState>();

		if (_currentGameLevel == null)
			return companionStates;

		// Find all player faction entities (excluding the player)
		var companions = _currentGameLevel.EntityManager.GetAllEntities()
			.Where(e => e.Faction == Faction.Player);

		foreach (var companion in companions)
		{
			var state = CompanionState.ExtractFromCompanion(companion);
			if (state != null)
			{
				companionStates.Add(state);
			}
		}

		return companionStates;
	}

	#endregion
}
