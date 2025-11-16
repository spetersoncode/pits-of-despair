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
    /// Spawn table IDs for creatures, indexed by floor depth (0-based).
    /// Example: [0] = "floor_1_creatures", [1] = "floor_2_creatures", etc.
    /// </summary>
    [Export]
    public string[] CreatureSpawnTableIdsByFloor { get; set; } = new[] { "floor_1_creatures" };

    /// <summary>
    /// Spawn table IDs for items, indexed by floor depth (0-based).
    /// Example: [0] = "floor_1_items", [1] = "floor_2_items", etc.
    /// </summary>
    [Export]
    public string[] ItemSpawnTableIdsByFloor { get; set; } = new[] { "floor_1_items" };

    private DataLoader? _dataLoader;
    private EntityFactory? _entityFactory;
    private EntityManager? _entityManager;
    private MapSystem? _mapSystem;
    private RandomNumberGenerator _rng;
    private int _currentFloor = 1;

    public override void _Ready()
    {
        _rng = new RandomNumberGenerator();
        _rng.Randomize();

        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
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
        PopulateItems();
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

            // Select random entry from spawn table
            var entry = SelectRandomSpawnEntry(spawnTable);
            if (entry == null)
                continue;

            // Determine how many creatures to spawn
            int creatureCount = _rng.RandiRange(entry.MinCount, entry.MaxCount);

            for (int i = 0; i < creatureCount; i++)
            {
                // Select random position in room
                var position = SelectRandomPosition(roomFloorTiles);

                // Spawn the creature
                Spawn(entry.CreatureId, position);
                totalCreatures++;
            }
        }
    }

    /// <summary>
    /// Populate dungeon with items based on current floor depth.
    /// Spawns items in rooms using the appropriate spawn table.
    /// </summary>
    public void PopulateItems()
    {
        if (_mapSystem == null)
        {
            GD.PushError("SpawnManager: Cannot populate items, MapSystem not set");
            return;
        }

        // Get spawn table for current floor
        var spawnTable = GetItemSpawnTable();
        if (spawnTable == null)
        {
            GD.PushWarning($"SpawnManager: No item spawn table for floor {_currentFloor}");
            return;
        }

        // Get all rooms
        var roomTiles = _mapSystem.GetRoomFloorTiles();
        int totalItems = 0;

        // Spawn items in each room
        foreach (var roomFloorTiles in roomTiles)
        {
            if (roomFloorTiles.Count == 0)
                continue;

            // Select random entry from spawn table
            var entry = SelectRandomItemSpawnEntry(spawnTable);
            if (entry == null)
                continue;

            // Determine how many items to spawn
            int itemCount = _rng.RandiRange(entry.MinCount, entry.MaxCount);

            for (int i = 0; i < itemCount; i++)
            {
                // Select random position in room that's not occupied
                var position = FindUnoccupiedPosition(roomFloorTiles);
                if (position != null)
                {
                    // Spawn the item
                    SpawnItem(entry.ItemId, position.Value);
                    totalItems++;
                }
            }
        }
    }

    /// <summary>
    /// Spawn a single entity at the specified position.
    /// Works for any entity type (creature, item, furniture, decoration).
    /// </summary>
    /// <param name="creatureId">The creature ID to spawn.</param>
    /// <param name="position">The grid position to spawn at.</param>
    /// <returns>The spawned entity, or null if spawning failed.</returns>
    public BaseEntity? Spawn(string creatureId, GridPosition position)
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
        var entity = _entityFactory.CreateEntity(creatureId, position);
        if (entity == null)
            return null;

        // Register with entity manager
        _entityManager.AddEntity(entity);

        return entity;
    }

    /// <summary>
    /// DEPRECATED: Spawn a single entity using old EntityData resource.
    /// </summary>
    [System.Obsolete("Use Spawn(string creatureId, GridPosition position) instead")]
    public BaseEntity? Spawn(EntityData entityData, GridPosition position)
    {
        return Spawn(entityData.Name.ToLower(), position);
    }

    /// <summary>
    /// Get the creature spawn table for the current floor.
    /// </summary>
    /// <returns>The spawn table, or null if not available.</returns>
    private CreatureSpawnTable? GetCreatureSpawnTable()
    {
        if (_dataLoader == null)
            return null;

        if (CreatureSpawnTableIdsByFloor == null || CreatureSpawnTableIdsByFloor.Length == 0)
            return null;

        int index = _currentFloor - 1; // Convert to 0-based index

        // Clamp to available tables
        if (index < 0)
            index = 0;
        if (index >= CreatureSpawnTableIdsByFloor.Length)
            index = CreatureSpawnTableIdsByFloor.Length - 1;

        string tableId = CreatureSpawnTableIdsByFloor[index];
        return _dataLoader.GetSpawnTable(tableId);
    }

    /// <summary>
    /// Select a random spawn entry from a spawn table using weighted random selection.
    /// </summary>
    private CreatureSpawnTableEntry? SelectRandomSpawnEntry(CreatureSpawnTable table)
    {
        if (table.Entries.Count == 0)
            return null;

        // Calculate total weight
        int totalWeight = 0;
        foreach (var entry in table.Entries)
        {
            totalWeight += entry.Weight;
        }

        // Select random value
        int roll = _rng.RandiRange(1, totalWeight);

        // Find the entry that matches the roll
        int currentWeight = 0;
        foreach (var entry in table.Entries)
        {
            currentWeight += entry.Weight;
            if (roll <= currentWeight)
            {
                return entry;
            }
        }

        // Fallback to first entry (should never happen)
        return table.Entries[0];
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

    /// <summary>
    /// Find an unoccupied position from a list of positions.
    /// </summary>
    /// <param name="positions">List of valid positions.</param>
    /// <returns>An unoccupied position, or null if all positions are occupied.</returns>
    private GridPosition? FindUnoccupiedPosition(System.Collections.Generic.List<GridPosition> positions)
    {
        // Try up to 10 times to find an unoccupied position
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var position = SelectRandomPosition(positions);
            if (_entityManager != null && !_entityManager.IsPositionOccupied(position))
            {
                return position;
            }
        }

        // If we couldn't find one after 10 attempts, do a linear search
        foreach (var position in positions)
        {
            if (_entityManager != null && !_entityManager.IsPositionOccupied(position))
            {
                return position;
            }
        }

        return null;
    }

    /// <summary>
    /// Spawn a single item at the specified position.
    /// </summary>
    /// <param name="itemId">The item ID to spawn.</param>
    /// <param name="position">The grid position to spawn at.</param>
    /// <returns>The spawned item, or null if spawning failed.</returns>
    public Item? SpawnItem(string itemId, GridPosition position)
    {
        if (_entityFactory == null)
        {
            GD.PushError("SpawnManager: Cannot spawn item, EntityFactory not set");
            return null;
        }

        if (_entityManager == null)
        {
            GD.PushError("SpawnManager: Cannot spawn item, EntityManager not set");
            return null;
        }

        // Create item via factory
        var item = _entityFactory.CreateItem(itemId, position);
        if (item == null)
            return null;

        // Register with entity manager
        _entityManager.AddEntity(item);

        return item;
    }

    /// <summary>
    /// Get the item spawn table for the current floor.
    /// </summary>
    /// <returns>The item spawn table, or null if not available.</returns>
    private ItemSpawnTable? GetItemSpawnTable()
    {
        if (_dataLoader == null)
            return null;

        if (ItemSpawnTableIdsByFloor == null || ItemSpawnTableIdsByFloor.Length == 0)
            return null;

        int index = _currentFloor - 1; // Convert to 0-based index

        // Clamp to available tables
        if (index < 0)
            index = 0;
        if (index >= ItemSpawnTableIdsByFloor.Length)
            index = ItemSpawnTableIdsByFloor.Length - 1;

        string tableId = ItemSpawnTableIdsByFloor[index];
        return _dataLoader.GetItemSpawnTable(tableId);
    }

    /// <summary>
    /// Select a random item spawn entry from a spawn table using weighted random selection.
    /// </summary>
    private ItemSpawnTableEntry? SelectRandomItemSpawnEntry(ItemSpawnTable table)
    {
        if (table.Entries.Count == 0)
            return null;

        // Calculate total weight
        int totalWeight = 0;
        foreach (var entry in table.Entries)
        {
            totalWeight += entry.Weight;
        }

        // Select random value
        int roll = _rng.RandiRange(1, totalWeight);

        // Find the entry that matches the roll
        int currentWeight = 0;
        foreach (var entry in table.Entries)
        {
            currentWeight += entry.Weight;
            if (roll <= currentWeight)
            {
                return entry;
            }
        }

        // Fallback to first entry (should never happen)
        return table.Entries[0];
    }

    // Future spawning methods:

    // public void PopulateFurniture() { }
    // public void PopulateDecorations() { }
    // public void SpawnPack(EntityData leader, EntityData member, GridPosition center, int count) { }
    // public void SpawnBoss(EntityData bossData, GridPosition position) { }
}
