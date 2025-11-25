using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Debug;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Spawning;
using PitsOfDespair.Systems.VisualEffects;
using PitsOfDespair.UI;
using System.Collections.Generic;

namespace PitsOfDespair.Systems;

/// <summary>
/// Main game level controller that wires together all game systems.
/// Orchestrates initialization flow for component-based entity system.
/// Represents a single dungeon floor that can be reused with different depths.
/// </summary>
public partial class GameLevel : Node
{
    /// <summary>
    /// Current floor depth (1-based). Used for difficulty scaling.
    /// </summary>
    [Export]
    public int FloorDepth { get; set; } = 1;

    // Public accessors for systems (used by Action system)
    public MapSystem MapSystem => _mapSystem;
    public EntityManager EntityManager => _entityManager;
    public Player Player => _player;
    public CombatSystem CombatSystem => _combatSystem;

    private MapSystem _mapSystem;
    private TextRenderer _renderer;
    private Player _player;
    private InputHandler _inputHandler;
    private MovementSystem _movementSystem;
    private EntityManager _entityManager;
    private EntityFactory _entityFactory;
    private SpawnOrchestrator _spawnOrchestrator;
    private PlayerVisionSystem _visionSystem;
    private NonPlayerVisionSystem _nonPlayerVisionSystem;
    private CombatSystem _combatSystem;
    private TurnManager _turnManager;
    private AISystem _aiSystem;
    private GameHUD _gameHUD;
    private CursorTargetingSystem _cursorSystem;
    private VisualEffectSystem _visualEffectSystem;
    private GoldManager _goldManager;

    // New systems for decoupling
    private LevelUpSystem _levelUpSystem;
    private PlayerActionHandler _actionHandler;
    private ViewModels.PlayerStatsViewModel _playerStatsViewModel;
    private ViewModels.EquipmentViewModel _equipmentViewModel;
    private NearbyEntitiesTracker _nearbyEntitiesTracker;
    private AutoExploreSystem _autoExploreSystem;
    private AutoRestSystem _autoRestSystem;
    private MessageSystem _messageSystem;
    private TileHazardManager _tileHazardManager;
    private TimeSystem _timeSystem;

    public override void _Ready()
    {
        _mapSystem = GetNode<MapSystem>("MapSystem");
        _renderer = GetNode<TextRenderer>("TextRenderer");
        _player = GetNode<Player>("Player");
        _inputHandler = GetNode<InputHandler>("InputHandler");
        _movementSystem = GetNode<MovementSystem>("MovementSystem");
        _entityManager = GetNode<EntityManager>("EntityManager");
        _entityFactory = GetNode<EntityFactory>("EntityFactory");
        _spawnOrchestrator = GetNode<SpawnOrchestrator>("SpawnOrchestrator");
        _visionSystem = GetNode<PlayerVisionSystem>("PlayerVisionSystem");
        _nonPlayerVisionSystem = GetNode<NonPlayerVisionSystem>("NonPlayerVisionSystem");
        _combatSystem = GetNode<CombatSystem>("CombatSystem");
        _turnManager = GetNode<TurnManager>("TurnManager");
        _aiSystem = GetNode<AISystem>("AISystem");
        _gameHUD = GetNode<GameHUD>("HUD/GameHUD");

        // GoldManager is now provided by GameManager (if it exists)
        // For standalone GameLevel (e.g., testing), create a local one
        _goldManager = GetTree()?.Root.GetNodeOrNull<GameManager>("GameManager")?.GetGoldManager();
        if (_goldManager == null)
        {
            _goldManager = new GoldManager { Name = "GoldManager" };
            AddChild(_goldManager);
        }

        _cursorSystem = new CursorTargetingSystem { Name = "CursorTargetingSystem" };
        AddChild(_cursorSystem);

        _visualEffectSystem = new VisualEffectSystem { Name = "VisualEffectSystem" };
        AddChild(_visualEffectSystem);

        // Create new systems for decoupling
        _levelUpSystem = new LevelUpSystem { Name = "LevelUpSystem" };
        AddChild(_levelUpSystem);

        _actionHandler = new PlayerActionHandler { Name = "PlayerActionHandler" };
        AddChild(_actionHandler);

        _playerStatsViewModel = new ViewModels.PlayerStatsViewModel { Name = "PlayerStatsViewModel" };
        AddChild(_playerStatsViewModel);

        _equipmentViewModel = new ViewModels.EquipmentViewModel();
        _equipmentViewModel.Name = "EquipmentViewModel";
        AddChild(_equipmentViewModel);

        _nearbyEntitiesTracker = new NearbyEntitiesTracker { Name = "NearbyEntitiesTracker" };
        AddChild(_nearbyEntitiesTracker);

        _autoExploreSystem = new AutoExploreSystem { Name = "AutoExploreSystem" };
        AddChild(_autoExploreSystem);

        _autoRestSystem = new AutoRestSystem { Name = "AutoRestSystem" };
        AddChild(_autoRestSystem);

        _messageSystem = new MessageSystem { Name = "MessageSystem" };
        AddChild(_messageSystem);

        _tileHazardManager = new TileHazardManager { Name = "TileHazardManager" };
        AddChild(_tileHazardManager);

        _timeSystem = new TimeSystem { Name = "TimeSystem" };
        AddChild(_timeSystem);

        _movementSystem.SetMapSystem(_mapSystem);
        _movementSystem.SetEntityManager(_entityManager);

        _spawnOrchestrator.SetDependencies(_entityFactory, _entityManager, _mapSystem, FloorDepth);

        var playerSpawn = _mapSystem.GetValidSpawnPosition();
        _player.Initialize(playerSpawn);

        _entityFactory.InitializePlayerInventory(_player);

        // Spawn companions based on floor depth
        var gameManager = GetTree()?.Root.GetNodeOrNull<GameManager>("GameManager");
        var savedCompanions = gameManager?.GetSavedCompanionStates();

        if (savedCompanions != null && savedCompanions.Count > 0)
        {
            // Restore companions from saved states (floors > 1)
            RestoreCompanions(savedCompanions, playerSpawn);
            gameManager?.ClearSavedCompanionStates();
        }
        else if (FloorDepth == 1)
        {
            // Spawn initial cat companion on floor 1 only
            var catSpawn = new GridPosition(playerSpawn.X + 1, playerSpawn.Y);
            if (!_mapSystem.IsWalkable(catSpawn))
                catSpawn = new GridPosition(playerSpawn.X - 1, playerSpawn.Y);
            var cat = _entityFactory.CreateCreature("cat", catSpawn);
            if (cat != null)
            {
                _entityFactory.SetupAsFriendlyCompanion(cat, _player);
                _entityManager.AddEntity(cat);
            }
        }

        _player.SetEntityManager(_entityManager);
        _player.SetGoldManager(_goldManager);

        _entityManager.SetPlayer(_player);

        _movementSystem.SetPlayer(_player);

        var playerMovement = _player.GetNode<MovementComponent>("MovementComponent");
        if (playerMovement != null)
        {
            _movementSystem.RegisterMovementComponent(playerMovement);
        }
        else
        {
            GD.PushError("GameLevel: Player missing MovementComponent!");
        }

        var playerAttack = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (playerAttack != null)
        {
            _combatSystem.RegisterAttackComponent(playerAttack);
        }

        _renderer.SetMapSystem(_mapSystem);
        _renderer.SetPlayer(_player);
        _renderer.SetEntityManager(_entityManager);

        _visionSystem.Initialize(_mapSystem, _player);
        _renderer.SetPlayerVisionSystem(_visionSystem);

        _cursorSystem.Initialize(_visionSystem, _mapSystem, _entityManager);
        _renderer.SetCursorTargetingSystem(_cursorSystem);

        _visualEffectSystem.SetTextRenderer(_renderer);
        _renderer.SetVisualEffectSystem(_visualEffectSystem);

        // Connect turn manager to VFX system for turn coordination
        _turnManager.SetVisualEffectSystem(_visualEffectSystem);

        // Connect turn manager to message system for message sequencing
        _turnManager.SetMessageSystem(_messageSystem);

        // Connect turn manager to time system for energy-based scheduling
        _turnManager.SetTimeSystem(_timeSystem);

        // Connect turn manager to AI system for creature processing
        _turnManager.SetAISystem(_aiSystem);

        // Initialize tile hazard manager
        _tileHazardManager.SetDependencies(_entityManager, _turnManager, _renderer);

        var actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem, _entityFactory, _visualEffectSystem, _tileHazardManager);

        _inputHandler.SetPlayer(_player);
        _inputHandler.SetTurnManager(_turnManager);
        _inputHandler.SetActionContext(actionContext);
        _inputHandler.SetGameHUD(_gameHUD);
        _inputHandler.SetCursorTargetingSystem(_cursorSystem);

        // Initialize autoexplore system
        _autoExploreSystem.Initialize(_player, _mapSystem, _entityManager, _visionSystem, _turnManager, actionContext);
        _inputHandler.SetAutoExploreSystem(_autoExploreSystem);

        // Initialize auto-rest system
        _autoRestSystem.Initialize(_player, _entityManager, _visionSystem, _turnManager, actionContext);
        _inputHandler.SetAutoRestSystem(_autoRestSystem);

        _inputHandler.Connect(InputHandler.SignalName.InventoryToggleRequested, Callable.From(_gameHUD.ToggleInventory));
        _inputHandler.Connect(InputHandler.SignalName.ActivateItemRequested, Callable.From(_gameHUD.ShowActivateMenu));
        _inputHandler.Connect(InputHandler.SignalName.DropItemRequested, Callable.From(_gameHUD.ShowDropMenu));
        _inputHandler.Connect(InputHandler.SignalName.EquipMenuRequested, Callable.From(_gameHUD.ShowEquipMenu));
        _inputHandler.Connect(InputHandler.SignalName.HelpRequested, Callable.From(_gameHUD.ShowHelp));
        _inputHandler.Connect(InputHandler.SignalName.SkillsMenuRequested, Callable.From(_gameHUD.ShowSkillsMenu));
        _inputHandler.Connect(InputHandler.SignalName.DebugModeToggled, Callable.From(_gameHUD.ToggleDebugMode));
        _inputHandler.Connect(InputHandler.SignalName.DebugConsoleRequested, Callable.From(_gameHUD.RequestDebugConsole));
        _inputHandler.Connect(InputHandler.SignalName.OpenLevelUpRequested, Callable.From(_gameHUD.RequestLevelUp));

        _aiSystem.SetMapSystem(_mapSystem);
        _aiSystem.SetPlayer(_player);
        _aiSystem.SetEntityManager(_entityManager);
        _aiSystem.SetCombatSystem(_combatSystem);
        _aiSystem.SetEntityFactory(_entityFactory);
        _aiSystem.SetVisualEffectSystem(_visualEffectSystem);

        _spawnOrchestrator.PopulateFloor(playerSpawn);

        foreach (var entity in _entityManager.GetAllEntities())
        {
            var movement = entity.GetNodeOrNull<MovementComponent>("MovementComponent");
            if (movement != null)
            {
                _movementSystem.RegisterMovementComponent(movement);
            }

            var attack = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
            if (attack != null)
            {
                _combatSystem.RegisterAttackComponent(attack);
            }

            var speedComponent = entity.GetNodeOrNull<SpeedComponent>("SpeedComponent");
            if (speedComponent != null)
            {
                // Register player separately for special handling
                if (entity == _player)
                {
                    _timeSystem.RegisterPlayer(speedComponent);
                }
                else
                {
                    _timeSystem.RegisterCreature(speedComponent);
                }
            }
        }

        _nonPlayerVisionSystem.Initialize(_mapSystem, _player, _entityManager, _combatSystem);

        // Initialize new systems for decoupling
        _levelUpSystem.Initialize(_player);
        _actionHandler.Initialize(_player, actionContext);
        _playerStatsViewModel.Initialize(_player, _goldManager, FloorDepth);
        _equipmentViewModel.Initialize(_player);
        _nearbyEntitiesTracker.Initialize(_player, _entityManager, _visionSystem);

        // Create debug context for debug commands (composes ActionContext for core systems)
        var dataLoader = GetNode<Data.DataLoader>("/root/DataLoader");
        var debugContext = new DebugContext(
            actionContext,
            _turnManager,
            _visionSystem,
            _cursorSystem,
            dataLoader,
            _aiSystem,
            _movementSystem,
            _timeSystem
        );

        // Get persistent debug mode state from GameManager (if it exists)
        bool debugModeActive = gameManager?.GetDebugModeActive() ?? false;

        // Initialize GameHUD with new systems
        _gameHUD.Initialize(
            _player,
            _combatSystem,
            _entityManager,
            FloorDepth,
            _goldManager,
            _levelUpSystem,
            _actionHandler,
            _messageSystem,
            _visionSystem,
            debugContext,
            debugModeActive
        );
        _gameHUD.ConnectToCursorTargetingSystem(_cursorSystem);
        _gameHUD.ConnectToAutoExploreSystem(_autoExploreSystem);

        // Connect tile hazard manager to message system for damage messages
        _messageSystem.ConnectToTileHazardManager(_tileHazardManager);

        // Initialize SidePanel with ViewModels
        var sidePanel = _gameHUD.GetNode<UI.SidePanel>("HBoxContainer/SidePanel");
        sidePanel.Initialize(_player, _playerStatsViewModel, _equipmentViewModel, _nearbyEntitiesTracker);

        _turnManager.StartFirstPlayerTurn();
    }

    /// <summary>
    /// Restores companions from saved states, spawning them near the player.
    /// </summary>
    private void RestoreCompanions(List<CompanionState> companionStates, GridPosition playerSpawn)
    {
        const int companionRange = 3;

        foreach (var state in companionStates)
        {
            // Find a valid spawn position near the player
            var spawnPos = FindNearbyValidPosition(playerSpawn, companionRange);
            if (spawnPos == null)
            {
                GD.PushWarning($"GameLevel: Could not find spawn position for companion '{state.CreatureId}'");
                continue;
            }

            // Create the creature from its ID
            var companion = _entityFactory.CreateCreature(state.CreatureId, spawnPos.Value);
            if (companion == null)
            {
                GD.PushWarning($"GameLevel: Failed to create companion '{state.CreatureId}'");
                continue;
            }

            // Set up as friendly companion
            _entityFactory.SetupAsFriendlyCompanion(companion, _player);

            // Apply saved state (health, status effects)
            state.ApplyToCompanion(companion);

            // Register with entity manager
            _entityManager.AddEntity(companion);
        }
    }

    /// <summary>
    /// Finds a valid (walkable, unoccupied) position within range of the center.
    /// </summary>
    private GridPosition? FindNearbyValidPosition(GridPosition center, int range)
    {
        var validPositions = new List<GridPosition>();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var checkPos = new GridPosition(center.X + dx, center.Y + dy);

                // Check Chebyshev distance
                if (DistanceHelper.ChebyshevDistance(center, checkPos) > range)
                    continue;

                if (!_mapSystem.IsWalkable(checkPos))
                    continue;

                if (_entityManager.IsPositionOccupied(checkPos))
                    continue;

                // Check player position
                if (checkPos == center)
                    continue;

                validPositions.Add(checkPos);
            }
        }

        if (validPositions.Count == 0)
            return null;

        // Pick a random position
        int index = GD.RandRange(0, validPositions.Count - 1);
        return validPositions[index];
    }

    public override void _ExitTree()
    {
        // Disconnect from input handler signals
        if (_inputHandler != null)
        {
            _inputHandler.Disconnect(InputHandler.SignalName.InventoryToggleRequested, Callable.From(_gameHUD.ToggleInventory));
            _inputHandler.Disconnect(InputHandler.SignalName.ActivateItemRequested, Callable.From(_gameHUD.ShowActivateMenu));
            _inputHandler.Disconnect(InputHandler.SignalName.DropItemRequested, Callable.From(_gameHUD.ShowDropMenu));
            _inputHandler.Disconnect(InputHandler.SignalName.EquipMenuRequested, Callable.From(_gameHUD.ShowEquipMenu));
            _inputHandler.Disconnect(InputHandler.SignalName.HelpRequested, Callable.From(_gameHUD.ShowHelp));
            _inputHandler.Disconnect(InputHandler.SignalName.SkillsMenuRequested, Callable.From(_gameHUD.ShowSkillsMenu));
            _inputHandler.Disconnect(InputHandler.SignalName.DebugModeToggled, Callable.From(_gameHUD.ToggleDebugMode));
            _inputHandler.Disconnect(InputHandler.SignalName.DebugConsoleRequested, Callable.From(_gameHUD.RequestDebugConsole));
            _inputHandler.Disconnect(InputHandler.SignalName.OpenLevelUpRequested, Callable.From(_gameHUD.RequestLevelUp));
        }
    }
}
