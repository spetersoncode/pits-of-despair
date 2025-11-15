using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages spawning of all entities (creatures, items, furniture, decorations).
/// Handles spawn orchestration, positioning, and registration with EntityManager.
/// </summary>
public partial class SpawnManager : Node
{
    /// <summary>
    /// Spawn tables for creatures, indexed by floor depth (0-based).
    /// Example: [0] = Floor 1, [1] = Floor 2, etc.
    /// </summary>
    [Export]
    public SpawnTable[] CreatureSpawnTablesByFloor { get; set; } = System.Array.Empty<SpawnTable>();

    private EntityFactory? _entityFactory;
    private EntityManager? _entityManager;
    private MapSystem? _mapSystem;
    private RandomNumberGenerator _rng;
    private int _currentFloor = 1;

    public override void _Ready()
    {
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Set the entity factory for creating entities.
    /// </summary>
    public void SetEntityFactory(EntityFactory factory)
    {
        _entityFactory = factory;
    }

    /// <summary>
    /// Set the entity manager for registering spawned entities.
    /// </summary>
    public void SetEntityManager(EntityManager manager)
    {
        _entityManager = manager;
    }

    /// <summary>
    /// Set the map system for querying room data and positions.
    /// </summary>
    public void SetMapSystem(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;
    }

    /// <summary>
    /// Set the current floor depth for difficulty scaling.
    /// </summary>
    /// <param name="depth">Floor depth (1-based). Used to select appropriate spawn tables.</param>
    public void SetFloorDepth(int depth)
    {
        _currentFloor = depth;
    }

    /// <summary>
    /// Populate the entire dungeon with entities.
    /// Orchestrates all spawning categories (creatures, items, furniture).
    /// </summary>
    public void PopulateDungeon()
    {
        PopulateCreatures();
        // Future: PopulateItems();
        // Future: PopulateFurniture();
        // Future: PopulateDecorations();
    }

    /// <summary>
    /// Populate dungeon with creatures based on current floor depth.
    /// Spawns creatures in all rooms using the appropriate spawn table.
    /// </summary>
    public void PopulateCreatures()
    {
        if (_mapSystem == null)
        {
            GD.PushError("SpawnManager: Cannot populate creatures, MapSystem not set");
            return;
        }

        // Get spawn table for current floor
        var spawnTable = GetCreatureSpawnTable();
        if (spawnTable == null)
        {
            GD.PushWarning($"SpawnManager: No creature spawn table for floor {_currentFloor}");
            return;
        }

        // Get all rooms
        var roomTiles = _mapSystem.GetRoomFloorTiles();
        int totalCreatures = 0;

        // Spawn creatures in each room
        foreach (var roomFloorTiles in roomTiles)
        {
            if (roomFloorTiles.Count == 0)
                continue;

            // Determine how many creatures to spawn
            int creatureCount = spawnTable.GetSpawnCount(_rng);

            for (int i = 0; i < creatureCount; i++)
            {
                // Select random creature from spawn table
                var entityData = spawnTable.SelectRandom(_rng);
                if (entityData == null)
                    continue;

                // Select random position in room
                var position = SelectRandomPosition(roomFloorTiles);

                // Spawn the creature
                Spawn(entityData, position);
                totalCreatures++;
            }
        }

        GD.Print($"SpawnManager: Populated {roomTiles.Count} rooms with {totalCreatures} creatures on floor {_currentFloor}");
    }

    /// <summary>
    /// Spawn a single entity at the specified position.
    /// Works for any entity type (creature, item, furniture, decoration).
    /// </summary>
    /// <param name="entityData">The entity data defining what to spawn.</param>
    /// <param name="position">The grid position to spawn at.</param>
    /// <returns>The spawned entity, or null if spawning failed.</returns>
    public BaseEntity? Spawn(EntityData entityData, GridPosition position)
    {
        if (_entityFactory == null)
        {
            GD.PushError("SpawnManager: Cannot spawn, EntityFactory not set");
            return null;
        }

        if (_entityManager == null)
        {
            GD.PushError("SpawnManager: Cannot spawn, EntityManager not set");
            return null;
        }

        // Create entity via factory
        var entity = _entityFactory.CreateEntity(entityData, position);

        // Register with entity manager
        _entityManager.AddEntity(entity);

        return entity;
    }

    /// <summary>
    /// Get the creature spawn table for the current floor.
    /// </summary>
    /// <returns>The spawn table, or null if not available.</returns>
    private SpawnTable? GetCreatureSpawnTable()
    {
        if (CreatureSpawnTablesByFloor == null || CreatureSpawnTablesByFloor.Length == 0)
            return null;

        int index = _currentFloor - 1; // Convert to 0-based index

        // Clamp to available tables
        if (index < 0)
            index = 0;
        if (index >= CreatureSpawnTablesByFloor.Length)
            index = CreatureSpawnTablesByFloor.Length - 1;

        return CreatureSpawnTablesByFloor[index];
    }

    /// <summary>
    /// Select a random position from a list of positions.
    /// </summary>
    /// <param name="positions">List of valid positions.</param>
    /// <returns>A randomly selected position.</returns>
    private GridPosition SelectRandomPosition(System.Collections.Generic.List<GridPosition> positions)
    {
        int index = _rng.RandiRange(0, positions.Count - 1);
        return positions[index];
    }

    // Future spawning methods:

    // public void PopulateItems() { }
    // public void PopulateFurniture() { }
    // public void PopulateDecorations() { }
    // public void SpawnPack(EntityData leader, EntityData member, GridPosition center, int count) { }
    // public void SpawnBoss(EntityData bossData, GridPosition position) { }
}
