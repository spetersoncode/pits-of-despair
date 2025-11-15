using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

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

        // Initialize component-based systems
        // This must happen AFTER MapSystem._Ready() generates the map,
        // but Godot calls _Ready() on children before parents, so map is ready

        // Wire up the movement system
        _movementSystem.SetMapSystem(_mapSystem);

        // Wire up spawn manager
        _spawnManager.SetEntityFactory(_entityFactory);
        _spawnManager.SetEntityManager(_entityManager);
        _spawnManager.SetMapSystem(_mapSystem);
        _spawnManager.SetFloorDepth(FloorDepth);

        // Initialize player at valid spawn position
        var playerSpawn = _mapSystem.GetValidSpawnPosition();
        _player.Initialize(playerSpawn);

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

        // Wire up renderer
        _renderer.SetMapSystem(_mapSystem);
        _renderer.SetPlayer(_player);
        _renderer.SetEntityManager(_entityManager);

        // Initialize vision system
        _visionSystem.Initialize(_mapSystem, _player);
        _renderer.SetPlayerVisionSystem(_visionSystem);

        // Wire up input handler
        _inputHandler.SetPlayer(_player);

        // Populate dungeon with creatures, items, etc.
        _spawnManager.PopulateDungeon();

        // Register all entity movement components with movement system
        foreach (var entity in _entityManager.GetAllEntities())
        {
            var movement = entity.GetNodeOrNull<MovementComponent>("MovementComponent");
            if (movement != null)
            {
                _movementSystem.RegisterMovementComponent(movement);
            }
        }

        // Initialize non-player vision system
        _nonPlayerVisionSystem.Initialize(_mapSystem, _player, _entityManager);

        GD.Print("GameLevel: All systems initialized and wired");
    }
}
