using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Main game level controller that wires together all game systems.
/// Orchestrates initialization flow for component-based entity system.
/// </summary>
public partial class GameLevel : Node
{
    private MapSystem _mapSystem;
    private ASCIIRenderer _renderer;
    private Player _player;
    private InputHandler _inputHandler;
    private MovementSystem _movementSystem;
    private EntityManager _entityManager;
    private EntityFactory _entityFactory;

    public override void _Ready()
    {
        // Get references to child nodes
        _mapSystem = GetNode<MapSystem>("MapSystem");
        _renderer = GetNode<ASCIIRenderer>("ASCIIRenderer");
        _player = GetNode<Player>("Player");
        _inputHandler = GetNode<InputHandler>("InputHandler");
        _movementSystem = GetNode<MovementSystem>("MovementSystem");
        _entityManager = GetNode<EntityManager>("EntityManager");
        _entityFactory = GetNode<EntityFactory>("EntityFactory");

        // Initialize component-based systems
        // This must happen AFTER MapSystem._Ready() generates the map,
        // but Godot calls _Ready() on children before parents, so map is ready

        // Wire up the movement system
        _movementSystem.SetMapSystem(_mapSystem);

        // Wire up entity factory to entity manager
        _entityManager.SetEntityFactory(_entityFactory);

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

        // Wire up input handler
        _inputHandler.SetPlayer(_player);

        // Spawn goblins in all rooms (except player's starting room)
        var roomTiles = _mapSystem.GetRoomFloorTiles();
        _entityManager.SpawnGoblins(roomTiles, playerSpawn);

        // Register all entity movement components with movement system
        foreach (var entity in _entityManager.GetAllEntities())
        {
            var movement = entity.GetNodeOrNull<MovementComponent>("MovementComponent");
            if (movement != null)
            {
                _movementSystem.RegisterMovementComponent(movement);
            }
        }

        GD.Print("GameLevel: All systems initialized and wired");
    }
}
