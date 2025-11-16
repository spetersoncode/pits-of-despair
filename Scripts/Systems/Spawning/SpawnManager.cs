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
    /// Main entry point: Populates the dungeon with creatures and items using budget-based spawning.
    /// Works with any map topology (rooms, caves, open areas).
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

        // Get all walkable tiles from the map
        var allWalkableTiles = _mapSystem.GetAllWalkableTiles();
        if (allWalkableTiles == null || allWalkableTiles.Count == 0)
        {
            GD.PushWarning("SpawnOrchestrator: No walkable tiles found in dungeon");
            return;
        }

        GD.Print($"SpawnOrchestrator: Found {allWalkableTiles.Count} walkable tiles");

        // Create density controller
        var densityController = new SpawnDensityController(spawnTable);
        GD.Print(densityController.GetDebugInfo());

        // Get spawn budgets
        int creatureBudget = densityController.GetCreatureBudget();
        int itemBudget = densityController.GetItemBudget();
        GD.Print($"SpawnOrchestrator: Creature budget = {creatureBudget}, Item budget = {itemBudget}");

        // Populate creatures using budget-based approach
        int totalCreatures = PopulateCreatures(
            allWalkableTiles,
            spawnTable,
            creatureBudget
        );

        // Populate items using budget-based approach
        int totalItems = PopulateItems(
            allWalkableTiles,
            spawnTable,
            itemBudget
        );

        GD.Print($"SpawnOrchestrator: Spawned {totalCreatures} creatures and {totalItems} items");
    }

    /// <summary>
    /// Populates the dungeon with creatures using budget-based spawning.
    /// Finds suitable locations for each spawn based on space requirements.
    /// </summary>
    private int PopulateCreatures(
        List<GridPosition> allWalkableTiles,
        SpawnTableData spawnTable,
        int totalBudget)
    {
        int totalSpawned = 0;
        int remainingBudget = totalBudget;
        int attempts = 0;
        int maxAttempts = totalBudget * 10; // Allow multiple attempts to find suitable locations

        // Track all spawned positions for spacing
        var spawnedPositions = new List<GridPosition>();
        var occupiedPositions = new HashSet<Vector2I>();

        while (remainingBudget > 0 && attempts < maxAttempts)
        {
            attempts++;

            // Select pool (common, uncommon, rare)
            var pool = spawnTable.SelectRandomCreaturePool();
            if (pool == null)
            {
                continue;
            }

            // Select entry from pool
            var entry = pool.SelectRandomEntry();
            if (entry == null || !entry.IsValid())
            {
                continue;
            }

            // Calculate required space for this spawn
            int requiredSpace = CalculateRequiredSpace(entry);

            // Find a suitable spawn location
            var spawnLocation = FindSpawnLocation(
                allWalkableTiles,
                spawnedPositions,
                requiredSpace,
                minSpacing: 3
            );

            if (spawnLocation == null)
            {
                // Couldn't find a suitable location, try next entry
                continue;
            }

            // Get tiles around spawn location for placement
            var spawnArea = _mapSystem.FindContiguousArea(spawnLocation.Value, requiredSpace * requiredSpace);
            var availableTiles = spawnArea
                .Select(gp => new Vector2I(gp.X, gp.Y))
                .ToList();

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
                spawnedPositions.Add(spawnLocation.Value);

                GD.Print($"  Spawned {result.EntityCount}x {entry} at {spawnLocation.Value} (Budget: {remainingBudget}/{totalBudget})");

                // Reset attempt counter on successful spawn
                attempts = 0;
            }
        }

        if (remainingBudget > 0)
        {
            GD.Print($"SpawnOrchestrator: Could not spend {remainingBudget} budget (max attempts reached)");
        }

        return totalSpawned;
    }

    /// <summary>
    /// Calculates the required space (NxN area) for a spawn entry.
    /// </summary>
    private int CalculateRequiredSpace(SpawnEntryData entry)
    {
        // Use explicit minimum space if specified
        if (entry.MinimumSpace > 0)
        {
            return entry.MinimumSpace;
        }

        // Otherwise estimate based on spawn type
        return entry.Type switch
        {
            SpawnEntryType.Single => 1,  // Single creature needs 1x1
            SpawnEntryType.Multiple => CalculateSpaceForCount(entry.Count.Max),  // Calculate based on max spawn count
            SpawnEntryType.Band => 3,    // Bands need at least 3x3 for formation
            SpawnEntryType.Unique => 3,  // Uniques get some breathing room
            _ => 1
        };
    }

    /// <summary>
    /// Calculates the minimum NxN grid size needed to accommodate a given number of entities.
    /// Uses the ceiling of the square root to determine the grid dimension.
    /// </summary>
    /// <param name="maxCount">Maximum number of entities to spawn</param>
    /// <returns>Grid dimension (e.g., 2 for a 2x2 grid)</returns>
    private int CalculateSpaceForCount(int maxCount)
    {
        // Calculate minimum grid dimension needed for maxCount entities
        // Examples: 1 entity → 1x1, 2-4 entities → 2x2, 5-9 entities → 3x3
        return (int)System.Math.Ceiling(System.Math.Sqrt(maxCount));
    }

    /// <summary>
    /// Finds a suitable spawn location with required space and spacing from other spawns.
    /// </summary>
    private GridPosition? FindSpawnLocation(
        List<GridPosition> allWalkableTiles,
        List<GridPosition> existingSpawns,
        int requiredSpace,
        int minSpacing)
    {
        // Shuffle walkable tiles for random distribution
        var shuffledTiles = allWalkableTiles.OrderBy(_ => GD.Randi()).ToList();

        foreach (var candidate in shuffledTiles)
        {
            // Check if area is clear
            if (!_mapSystem.IsAreaClear(candidate, requiredSpace))
            {
                continue;
            }

            // Check spacing from existing spawns
            bool tooClose = false;
            foreach (var existing in existingSpawns)
            {
                int distance = Mathf.Abs(existing.X - candidate.X) + Mathf.Abs(existing.Y - candidate.Y);
                if (distance < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            // Found a suitable location
            return candidate;
        }

        // No suitable location found
        return null;
    }

    /// <summary>
    /// Populates the dungeon with items using budget-based spawning.
    /// </summary>
    private int PopulateItems(
        List<GridPosition> allWalkableTiles,
        SpawnTableData spawnTable,
        int totalBudget)
    {
        int totalSpawned = 0;
        int remainingBudget = totalBudget;
        int attempts = 0;
        int maxAttempts = totalBudget * 10; // Allow multiple attempts to find suitable locations

        var occupiedPositions = new HashSet<Vector2I>();

        // Mark positions occupied by creatures
        foreach (var gridPos in allWalkableTiles)
        {
            if (_entityManager.IsPositionOccupied(gridPos))
            {
                occupiedPositions.Add(new Vector2I(gridPos.X, gridPos.Y));
            }
        }

        var availableTiles = allWalkableTiles
            .Select(gp => new Vector2I(gp.X, gp.Y))
            .ToList();

        while (remainingBudget > 0 && attempts < maxAttempts)
        {
            attempts++;

            // Select pool (weighted random)
            var pool = spawnTable.SelectRandomItemPool();
            if (pool == null)
            {
                continue;
            }

            // Select entry from pool
            var entry = pool.SelectRandomEntry();
            if (entry == null || !entry.IsValid())
            {
                continue;
            }

            // Execute spawn using single strategy
            var strategy = _spawnStrategies["single"];
            var result = strategy.Execute(entry, availableTiles, occupiedPositions);

            if (result.Success)
            {
                totalSpawned += result.EntityCount;
                remainingBudget -= result.EntityCount;

                GD.Print($"  Spawned {result.EntityCount}x item {entry} (Budget: {remainingBudget}/{totalBudget})");

                // Reset attempt counter on successful spawn
                attempts = 0;
            }
        }

        if (remainingBudget > 0)
        {
            GD.Print($"SpawnOrchestrator: Could not spend {remainingBudget} item budget (max attempts reached)");
        }

        return totalSpawned;
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
            SpawnEntryType.Multiple => _spawnStrategies["single"],  // Multiple uses same strategy as single
            SpawnEntryType.Band => _spawnStrategies["band"],
            SpawnEntryType.Unique => _spawnStrategies["unique"],
            _ => null
        };
    }
}
