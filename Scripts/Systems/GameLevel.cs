using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
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
    private TargetingSystem _targetingSystem;
    private ProjectileSystem _projectileSystem;

    public override void _Ready()
    {
        // Get references to child nodes
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

        // Create targeting and projectile systems
        _targetingSystem = new TargetingSystem { Name = "TargetingSystem" };
        AddChild(_targetingSystem);

        _projectileSystem = new ProjectileSystem { Name = "ProjectileSystem" };
        AddChild(_projectileSystem);

        // Initialize component-based systems
        // This must happen AFTER MapSystem._Ready() generates the map,
        // but Godot calls _Ready() on children before parents, so map is ready

        // Wire up the movement system
        _movementSystem.SetMapSystem(_mapSystem);
        _movementSystem.SetEntityManager(_entityManager);

        // Wire up spawn manager
        _spawnManager.SetDependencies(_entityFactory, _entityManager, _mapSystem, FloorDepth);

        // Initialize player at valid spawn position
        var playerSpawn = _mapSystem.GetValidSpawnPosition();
        _player.Initialize(playerSpawn);

        // Wire up player's entity manager reference (for item pickup)
        _player.SetEntityManager(_entityManager);

        // Wire up player reference to movement system (for bump-to-attack)
        _movementSystem.SetPlayer(_player);

        // Register player's movement component with movement system
        var playerMovement = _player.GetNode<MovementComponent>("MovementComponent");
        if (playerMovement != null)
        {
            _movementSystem.RegisterMovementComponent(playerMovement);
        }
        else
        {
            GD.PushError("GameLevel: Player missing MovementComponent!");
        }

        // Register player's attack component with combat system
        var playerAttack = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (playerAttack != null)
        {
            _combatSystem.RegisterAttackComponent(playerAttack);
        }

        // Wire up renderer
        _renderer.SetMapSystem(_mapSystem);
        _renderer.SetPlayer(_player);
        _renderer.SetEntityManager(_entityManager);

        // Initialize vision system
        _visionSystem.Initialize(_mapSystem, _player);
        _renderer.SetPlayerVisionSystem(_visionSystem);

        // Initialize targeting system
        _targetingSystem.Initialize(_mapSystem, _entityManager);
        _renderer.SetTargetingSystem(_targetingSystem);

        // Initialize projectile system
        _projectileSystem.Initialize(_combatSystem);
        _projectileSystem.ConnectToPlayer(_player);
        _projectileSystem.SetTextRenderer(_renderer);
        _renderer.SetProjectileSystem(_projectileSystem);

        // Create action context for the action system
        var actionContext = new ActionContext(_mapSystem, _entityManager, _player, _combatSystem);

        // Wire up input handler
        _inputHandler.SetPlayer(_player);
        _inputHandler.SetTurnManager(_turnManager);
        _inputHandler.SetActionContext(actionContext);
        _inputHandler.SetGameHUD(_gameHUD);
        _inputHandler.SetPlayerVisionSystem(_visionSystem);
        _inputHandler.SetTargetingSystem(_targetingSystem);

        // Connect input handler signals to HUD
        _inputHandler.InventoryToggleRequested += _gameHUD.ToggleInventory;
        _inputHandler.ActivateItemRequested += _gameHUD.ShowActivateMenu;
        _inputHandler.DropItemRequested += _gameHUD.ShowDropMenu;
        _inputHandler.EquipMenuRequested += _gameHUD.ShowEquipMenu;

        // Wire up AI system
        _aiSystem.SetMapSystem(_mapSystem);
        _aiSystem.SetPlayer(_player);
        _aiSystem.SetEntityManager(_entityManager);
        _aiSystem.SetTurnManager(_turnManager);
        _aiSystem.SetCombatSystem(_combatSystem);

        // Populate dungeon with creatures, items, etc.
        _spawnManager.PopulateDungeon();

        // Register all entity components with appropriate systems
        foreach (var entity in _entityManager.GetAllEntities())
        {
            // Register movement components
            var movement = entity.GetNodeOrNull<MovementComponent>("MovementComponent");
            if (movement != null)
            {
                _movementSystem.RegisterMovementComponent(movement);
            }

            // Register attack components
            var attack = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
            if (attack != null)
            {
                _combatSystem.RegisterAttackComponent(attack);
            }

            // Register AI components
            var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
            if (aiComponent != null)
            {
                _aiSystem.RegisterAIComponent(aiComponent);
            }
        }

        // Initialize non-player vision system
        _nonPlayerVisionSystem.Initialize(_mapSystem, _player, _entityManager);

        // Initialize HUD
        _gameHUD.Initialize(_player, _combatSystem, _entityManager, FloorDepth, actionContext, _visionSystem);

        // Start the first player turn
        _turnManager.StartFirstPlayerTurn();
    }
}
