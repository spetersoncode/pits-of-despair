using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Systems.Spawning.Data;
using PitsOfDespair.Systems.Spawning.Placement;
using PitsOfDespair.Systems.Spawning.Strategies;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Manages all dungeon spawning using a strategy-based system.
/// DCSS/NetHack-inspired approach with flexible spawn mechanics.
///
/// Features:
/// - Multiple spawn types per room (common + rare)
/// - Monster bands with leaders and followers
/// - Spawn density control with empty rooms
/// - Out-of-depth spawns for danger/reward
/// - Intelligent placement strategies (formations, surrounding, etc.)
/// </summary>
public partial class SpawnManager : Node
{
    private EntityFactory _entityFactory;
    private EntityManager _entityManager;
    private MapSystem _mapSystem;
    private DataLoader _dataLoader;
    private int _floorDepth;

    // Strategy registries
    private readonly Dictionary<string, IPlacementStrategy> _placementStrategies = new();
    private readonly Dictionary<string, ISpawnStrategy> _spawnStrategies = new();

    // Spawn table configuration
    // For now, use floor_1 for all floors (will fallback to last table)
    private readonly string[] _spawnTableIdsByFloor = new[]
    {
        "floor_1"
    };

    public void SetDependencies(
        EntityFactory entityFactory,
        EntityManager entityManager,
        MapSystem mapSystem,
        int floorDepth)
    {
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _mapSystem = mapSystem;
        _floorDepth = floorDepth;
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");

        InitializeStrategies();
    }

    /// <summary>
    /// Initializes all placement and spawn strategies.
    /// </summary>
    private void InitializeStrategies()
    {
        // Register placement strategies
        _placementStrategies["random"] = new RandomPlacement();
        _placementStrategies["center"] = new CenterPlacement();
        _placementStrategies["surrounding"] = new SurroundingPlacement();
        _placementStrategies["formation"] = new FormationPlacement(FormationType.Line);

        // Register spawn strategies
        _spawnStrategies["single"] = new SingleSpawnStrategy(
            _entityFactory,
            _entityManager,
            _placementStrategies
        );

        _spawnStrategies["band"] = new BandSpawnStrategy(
            _entityFactory,
            _entityManager,
            _dataLoader,
            _placementStrategies
        );

        _spawnStrategies["unique"] = new UniqueSpawnStrategy(
            _entityFactory,
            _entityManager,
            _placementStrategies
        );
    }

    /// <summary>
    /// Main entry point: Populates the dungeon with creatures and items.
    /// </summary>
    public void PopulateDungeon()
    {
        GD.Print($"SpawnOrchestrator: Populating floor {_floorDepth}...");

        // Get spawn table for this floor
        var spawnTable = GetSpawnTableForFloor(_floorDepth);
        if (spawnTable == null)
        {
            GD.PushWarning($"SpawnOrchestrator: No spawn table found for floor {_floorDepth}");
            return;
        }

        GD.Print($"SpawnOrchestrator: Using spawn table '{spawnTable.Name}'");

        // Get room data from MapSystem
        var roomTiles = _mapSystem.GetRoomFloorTiles();
        if (roomTiles == null || roomTiles.Count == 0)
        {
            GD.PushWarning("SpawnOrchestrator: No rooms found in dungeon");
            return;
        }

        GD.Print($"SpawnOrchestrator: Found {roomTiles.Count} rooms");

        // Create density controller
        var densityController = new SpawnDensityController(spawnTable);
        GD.Print(densityController.GetDebugInfo());

        // Allocate spawn budget across rooms
        var allocations = densityController.AllocateSpawnBudget(roomTiles.Count);

        // Populate each room
        int totalCreatures = 0;
        int totalItems = 0;

        for (int i = 0; i < roomTiles.Count && i < allocations.Count; i++)
        {
            var allocation = allocations[i];
            var roomFloorTiles = roomTiles[i];

            if (allocation.IsEmpty)
            {
                GD.Print($"  Room {i}: Empty (by design)");
                continue;
            }

            GD.Print($"  Room {i}: Budget={allocation.SpawnBudget}, OOD={allocation.IsOutOfDepth}");

            // Populate creatures
            var creatureCount = PopulateRoomCreatures(
                roomFloorTiles,
                spawnTable,
                allocation.SpawnBudget,
                allocation.IsOutOfDepth
            );

            totalCreatures += creatureCount;

            // Populate items (separate from creature budget)
            var itemCount = PopulateRoomItems(roomFloorTiles, spawnTable);
            totalItems += itemCount;
        }

        GD.Print($"SpawnOrchestrator: Spawned {totalCreatures} creatures and {totalItems} items");
    }

    /// <summary>
    /// Populates a single room with creatures based on spawn budget.
    /// </summary>
    private int PopulateRoomCreatures(
        List<GridPosition> roomFloorTiles,
        SpawnTableData spawnTable,
        int spawnBudget,
        bool outOfDepth)
    {
        int totalSpawned = 0;
        var occupiedPositions = new HashSet<Vector2I>();

        // Convert GridPosition to Vector2I for placement strategies
        var availableTiles = roomFloorTiles
            .Select(gp => new Vector2I(gp.X, gp.Y))
            .ToList();

        // Spawn multiple entries until budget exhausted
        int remainingBudget = spawnBudget;
        int attempts = 0;
        int maxAttempts = 50; // Prevent infinite loops

        while (remainingBudget > 0 && attempts < maxAttempts)
        {
            attempts++;

            // Select pool (common, uncommon, rare, or out-of-depth)
            var pool = spawnTable.SelectRandomCreaturePool(outOfDepth);
            if (pool == null)
            {
                break;
            }

            // Select entry from pool
            var entry = pool.SelectRandomEntry();
            if (entry == null || !entry.IsValid())
            {
                continue;
            }

            // Get spawn strategy for this entry type
            var strategy = GetSpawnStrategy(entry.Type);
            if (strategy == null)
            {
                GD.PushWarning($"SpawnOrchestrator: No strategy for type '{entry.Type}'");
                continue;
            }

            // Execute spawn
            var result = strategy.Execute(entry, availableTiles, occupiedPositions);

            if (result.Success)
            {
                totalSpawned += result.EntityCount;
                remainingBudget -= result.EntityCount;

                GD.Print($"    Spawned {result.EntityCount}x {entry} (Budget: {remainingBudget}/{spawnBudget})");
            }
        }

        return totalSpawned;
    }

    /// <summary>
    /// Populates a single room with items.
    /// </summary>
    private int PopulateRoomItems(List<GridPosition> roomFloorTiles, SpawnTableData spawnTable)
    {
        // Items don't use spawn budget - they have their own spawn chance per room
        var pool = spawnTable.SelectRandomItemPool();
        if (pool == null)
        {
            return 0;
        }

        var entry = pool.SelectRandomEntry();
        if (entry == null || !entry.IsValid())
        {
            return 0;
        }

        var occupiedPositions = new HashSet<Vector2I>();

        // Mark positions occupied by creatures
        foreach (var gridPos in roomFloorTiles)
        {
            if (_entityManager.IsPositionOccupied(gridPos))
            {
                occupiedPositions.Add(new Vector2I(gridPos.X, gridPos.Y));
            }
        }

        var availableTiles = roomFloorTiles
            .Select(gp => new Vector2I(gp.X, gp.Y))
            .ToList();

        var strategy = _spawnStrategies["single"];
        var result = strategy.Execute(entry, availableTiles, occupiedPositions);

        return result.EntityCount;
    }

    /// <summary>
    /// Gets spawn table for the specified floor depth.
    /// </summary>
    private SpawnTableData GetSpawnTableForFloor(int floorDepth)
    {
        // Use floor depth as index, with fallback to last table
        int index = Mathf.Clamp(floorDepth, 0, _spawnTableIdsByFloor.Length - 1);
        string spawnTableId = _spawnTableIdsByFloor[index];

        return _dataLoader.GetSpawnTable(spawnTableId);
    }

    /// <summary>
    /// Gets spawn strategy for the specified entry type.
    /// </summary>
    private ISpawnStrategy GetSpawnStrategy(SpawnEntryType entryType)
    {
        return entryType switch
        {
            SpawnEntryType.Single => _spawnStrategies["single"],
            SpawnEntryType.Band => _spawnStrategies["band"],
            SpawnEntryType.Unique => _spawnStrategies["unique"],
            _ => null
        };
    }
}
