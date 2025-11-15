using Godot;
using Godot.Collections;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages all non-player entities in the game.
/// Handles entity spawning, tracking, and lifecycle.
/// </summary>
public partial class EntityManager : Node
{
    /// <summary>
    /// Emitted when an entity is added to the manager.
    /// </summary>
    [Signal]
    public delegate void EntityAddedEventHandler(BaseEntity entity);

    /// <summary>
    /// Emitted when an entity is removed from the manager.
    /// </summary>
    [Signal]
    public delegate void EntityRemovedEventHandler(BaseEntity entity);

    private readonly System.Collections.Generic.List<BaseEntity> _entities = new();
    private EntityFactory? _entityFactory;

    /// <summary>
    /// Set the EntityFactory reference for creating entities.
    /// </summary>
    /// <param name="factory">The EntityFactory to use.</param>
    public void SetEntityFactory(EntityFactory factory)
    {
        _entityFactory = factory;
    }

    /// <summary>
    /// Spawn goblins in each room of the dungeon.
    /// Spawns 1-3 goblins per room at random floor tile positions.
    /// Skips the room containing the player's spawn position to keep it clear.
    /// </summary>
    /// <param name="roomTiles">List of rooms, where each room is a list of floor tile positions.</param>
    /// <param name="playerSpawnPosition">The player's starting position - goblins won't spawn in this room.</param>
    public void SpawnGoblins(System.Collections.Generic.List<System.Collections.Generic.List<GridPosition>> roomTiles, GridPosition playerSpawnPosition)
    {
        if (_entityFactory == null)
        {
            GD.PushError("EntityManager: Cannot spawn goblins, EntityFactory not set");
            return;
        }

        // Load goblin data resource
        var goblinData = GD.Load<EntityData>("res://Resources/Entities/goblin.tres");
        if (goblinData == null)
        {
            GD.PushError("EntityManager: Failed to load goblin.tres resource");
            return;
        }

        var random = new RandomNumberGenerator();
        random.Randomize();

        int roomsProcessed = 0;

        // Spawn goblins in each room
        foreach (var roomFloorTiles in roomTiles)
        {
            if (roomFloorTiles.Count == 0)
                continue;

            // Skip the room containing the player's spawn position
            bool isPlayerRoom = roomFloorTiles.Contains(playerSpawnPosition);
            if (isPlayerRoom)
            {
                GD.Print("EntityManager: Skipping goblin spawn in player's starting room");
                continue;
            }

            // Spawn 1-3 goblins per room
            int goblinCount = random.RandiRange(1, 3);

            for (int i = 0; i < goblinCount; i++)
            {
                // Pick a random floor tile in the room
                int tileIndex = random.RandiRange(0, roomFloorTiles.Count - 1);
                var spawnPos = roomFloorTiles[tileIndex];

                // Create goblin entity
                var goblin = _entityFactory.CreateEntity(goblinData, spawnPos);

                // Add to scene tree and tracking list
                AddChild(goblin);
                _entities.Add(goblin);

                // Emit signal for other systems (renderer, movement system, etc.)
                EmitSignal(SignalName.EntityAdded, goblin);
            }

            roomsProcessed++;
        }

        GD.Print($"EntityManager: Spawned goblins in {roomsProcessed} rooms (total: {_entities.Count} entities, skipped player's room)");
    }

    /// <summary>
    /// Get all entities managed by this system.
    /// </summary>
    /// <returns>Read-only list of all entities.</returns>
    public System.Collections.Generic.IReadOnlyList<BaseEntity> GetAllEntities()
    {
        return _entities.AsReadOnly();
    }

    /// <summary>
    /// Remove an entity from management and the scene.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void RemoveEntity(BaseEntity entity)
    {
        if (_entities.Remove(entity))
        {
            EmitSignal(SignalName.EntityRemoved, entity);
            entity.QueueFree();
        }
    }
}
