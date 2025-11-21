using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Debug;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Spawning;
using PitsOfDespair.UI;

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
    private SpawnManager _spawnManager;
    private PlayerVisionSystem _visionSystem;
    private NonPlayerVisionSystem _nonPlayerVisionSystem;
    private CombatSystem _combatSystem;
    private TurnManager _turnManager;
    private AISystem _aiSystem;
    private GameHUD _gameHUD;
    private CursorTargetingSystem _cursorSystem;
    private ProjectileSystem _projectileSystem;
    private GoldManager _goldManager;

    // New systems for decoupling
    private LevelUpSystem _levelUpSystem;
    private PlayerActionHandler _actionHandler;
    private ViewModels.PlayerStatsViewModel _playerStatsViewModel;
    private ViewModels.EquipmentViewModel _equipmentViewModel;
    private NearbyEntitiesTracker _nearbyEntitiesTracker;

    public override void _Ready()
    {
        _mapSystem = GetNode<MapSystem>("MapSystem");
        _renderer = GetNode<TextRenderer>("TextRenderer");
        _player = GetNode<Player>("Player");
        _inputHandler = GetNode<InputHandler>("InputHandler");
        _movementSystem = GetNode<MovementSystem>("MovementSystem");
        _entityManager = GetNode<EntityManager>("EntityManager");
        _entityFactory = GetNode<EntityFactory>("EntityFactory");
        _spawnManager = GetNode<SpawnManager>("SpawnManager");
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

        _projectileSystem = new ProjectileSystem { Name = "ProjectileSystem" };
        AddChild(_projectileSystem);

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

        _movementSystem.SetMapSystem(_mapSystem);
        _movementSystem.SetEntityManager(_entityManager);

        _spawnManager.SetDependencies(_entityFactory, _entityManager, _mapSystem, FloorDepth);

        var playerSpawn = _mapSystem.GetValidSpawnPosition();
        _player.Initialize(playerSpawn);

        _entityFactory.InitializePlayerInventory(_player);

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

        _projectileSystem.Initialize(_combatSystem);
        _projectileSystem.ConnectToPlayer(_player);
        _projectileSystem.SetTextRenderer(_renderer);
        _renderer.SetProjectileSystem(_projectileSystem);

        var actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem, _entityFactory, _projectileSystem);

        _inputHandler.SetPlayer(_player);
        _inputHandler.SetTurnManager(_turnManager);
        _inputHandler.SetActionContext(actionContext);
        _inputHandler.SetGameHUD(_gameHUD);
        _inputHandler.SetCursorTargetingSystem(_cursorSystem);

        _inputHandler.Connect(InputHandler.SignalName.InventoryToggleRequested, Callable.From(_gameHUD.ToggleInventory));
        _inputHandler.Connect(InputHandler.SignalName.ActivateItemRequested, Callable.From(_gameHUD.ShowActivateMenu));
        _inputHandler.Connect(InputHandler.SignalName.DropItemRequested, Callable.From(_gameHUD.ShowDropMenu));
        _inputHandler.Connect(InputHandler.SignalName.EquipMenuRequested, Callable.From(_gameHUD.ShowEquipMenu));
        _inputHandler.Connect(InputHandler.SignalName.HelpRequested, Callable.From(_gameHUD.ShowHelp));
        _inputHandler.Connect(InputHandler.SignalName.DebugModeToggled, Callable.From(_gameHUD.ToggleDebugMode));
        _inputHandler.Connect(InputHandler.SignalName.DebugConsoleRequested, Callable.From(_gameHUD.RequestDebugConsole));

        _aiSystem.SetMapSystem(_mapSystem);
        _aiSystem.SetPlayer(_player);
        _aiSystem.SetEntityManager(_entityManager);
        _aiSystem.SetTurnManager(_turnManager);
        _aiSystem.SetCombatSystem(_combatSystem);
        _aiSystem.SetEntityFactory(_entityFactory);
        _aiSystem.SetProjectileSystem(_projectileSystem);

        _spawnManager.PopulateDungeon(playerSpawn);

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

            var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
            if (aiComponent != null)
            {
                _aiSystem.RegisterAIComponent(aiComponent);
            }
        }

        _nonPlayerVisionSystem.Initialize(_mapSystem, _player, _entityManager);

        // Initialize new systems for decoupling
        _levelUpSystem.Initialize(_player);
        _actionHandler.Initialize(_player, actionContext);
        _playerStatsViewModel.Initialize(_player, _goldManager, FloorDepth);
        _equipmentViewModel.Initialize(_player);
        _nearbyEntitiesTracker.Initialize(_player, _entityManager, _visionSystem);

        // Create debug context for debug commands (composes ActionContext for core systems)
        var debugContext = new DebugContext(
            actionContext,
            _turnManager,
            _visionSystem
        );

        // Get persistent debug mode state from GameManager (if it exists)
        bool debugModeActive = false;
        var gameManager = GetTree()?.Root.GetNodeOrNull<GameManager>("GameManager");
        if (gameManager != null)
        {
            debugModeActive = gameManager.GetDebugModeActive();
        }

        // Initialize GameHUD with new systems
        _gameHUD.Initialize(
            _player,
            _combatSystem,
            _entityManager,
            FloorDepth,
            _goldManager,
            _levelUpSystem,
            _actionHandler,
            _visionSystem,
            debugContext,
            debugModeActive
        );
        _gameHUD.ConnectToCursorTargetingSystem(_cursorSystem);

        // Initialize SidePanel with ViewModels
        var sidePanel = _gameHUD.GetNode<UI.SidePanel>("HBoxContainer/SidePanel");
        sidePanel.Initialize(_player, _entityManager, _playerStatsViewModel, _equipmentViewModel, _nearbyEntitiesTracker);

        _turnManager.StartFirstPlayerTurn();
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
            _inputHandler.Disconnect(InputHandler.SignalName.DebugModeToggled, Callable.From(_gameHUD.ToggleDebugMode));
            _inputHandler.Disconnect(InputHandler.SignalName.DebugConsoleRequested, Callable.From(_gameHUD.RequestDebugConsole));
        }
    }
}
