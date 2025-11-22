using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;
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
    // Maps floor depth (1-10) to spawn table IDs
    // Array is 0-indexed but represents floors 1-10
    private readonly string[] _spawnTableIdsByFloor = new[]
    {
        "floor_1",   // Floor 1
        "floor_2",   // Floor 2
        "floor_3",   // Floor 3
        "floor_4",   // Floor 4
        "floor_5",   // Floor 5
        "floor_6",   // Floor 6
        "floor_7",   // Floor 7
        "floor_8",   // Floor 8
        "floor_9",   // Floor 9
        "floor_10"   // Floor 10
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
    /// <param name="playerPosition">Player's starting position to enforce exclusion zone</param>
    public void PopulateDungeon(GridPosition playerPosition)
    {

        // Get spawn table for this floor
        var spawnTable = GetSpawnTableForFloor(_floorDepth);
        if (spawnTable == null)
        {
            GD.PushWarning($"SpawnOrchestrator: No spawn table found for floor {_floorDepth}");
            return;
        }


        // Get all walkable tiles from the map
        var allWalkableTiles = _mapSystem.GetAllWalkableTiles();
        if (allWalkableTiles == null || allWalkableTiles.Count == 0)
        {
            GD.PushWarning("SpawnOrchestrator: No walkable tiles found in dungeon");
            return;
        }


        // Create density controller
        var densityController = new SpawnDensityController(spawnTable);

        // Get spawn budgets
        int creatureBudget = densityController.GetCreatureBudget();
        int itemBudget = densityController.GetItemBudget();
        int goldBudget = spawnTable.GetRandomGoldBudget();

        // Populate creatures using budget-based approach
        int totalCreatures = PopulateCreatures(
            allWalkableTiles,
            spawnTable,
            creatureBudget,
            playerPosition
        );

        // Populate items using budget-based approach
        int totalItems = PopulateItems(
            allWalkableTiles,
            spawnTable,
            itemBudget
        );

        // Populate gold piles
        int totalGold = PopulateGold(
            allWalkableTiles,
            goldBudget
        );

        // Spawn stairs or throne depending on floor depth (far from player)
        SpawnStairsOrThrone(allWalkableTiles, playerPosition);
    }

    /// <summary>
    /// Populates the dungeon with creatures using budget-based spawning.
    /// Finds suitable locations for each spawn based on space requirements.
    /// </summary>
    /// <param name="playerPosition">Player's position to enforce exclusion zone</param>
    private int PopulateCreatures(
        List<GridPosition> allWalkableTiles,
        SpawnTableData spawnTable,
        int totalBudget,
        GridPosition playerPosition)
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

            // Find a suitable spawn location with variable spacing and player exclusion
            int variableSpacing = GD.RandRange(5, 10);
            var spawnLocation = FindSpawnLocation(
                allWalkableTiles,
                spawnedPositions,
                requiredSpace,
                variableSpacing,
                playerPosition
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


                // Reset attempt counter on successful spawn
                attempts = 0;
            }
        }

        if (remainingBudget > 0)
        {
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
            SpawnEntryType.Multiple => CalculateSpaceForCount(entry.Count.GetMax()),  // Calculate based on max spawn count
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
    /// Enforces minimum distance from player using Euclidean distance.
    /// </summary>
    /// <param name="playerPosition">Player's position to enforce exclusion zone</param>
    private GridPosition? FindSpawnLocation(
        List<GridPosition> allWalkableTiles,
        List<GridPosition> existingSpawns,
        int requiredSpace,
        int minSpacing,
        GridPosition playerPosition)
    {
        // Player exclusion zone (12-15 tiles, use 13 as middle value)
        const int playerExclusionRadius = 13;
        int playerExclusionRadiusSquared = playerExclusionRadius * playerExclusionRadius;

        // Shuffle walkable tiles for random distribution
        var shuffledTiles = allWalkableTiles.OrderBy(_ => GD.Randi()).ToList();

        foreach (var candidate in shuffledTiles)
        {
            // Check if area is clear
            if (!_mapSystem.IsAreaClear(candidate, requiredSpace))
            {
                continue;
            }

            // Check distance from player (Euclidean distance for circular exclusion zone)
            int distanceToPlayerSquared = DistanceHelper.EuclideanDistance(candidate, playerPosition);
            if (distanceToPlayerSquared < playerExclusionRadiusSquared)
            {
                continue;
            }

            // Check spacing from existing spawns (Euclidean distance)
            int minSpacingSquared = minSpacing * minSpacing;
            bool tooClose = false;
            foreach (var existing in existingSpawns)
            {
                int distanceSquared = DistanceHelper.EuclideanDistance(existing, candidate);
                if (distanceSquared < minSpacingSquared)
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


                // Reset attempt counter on successful spawn
                attempts = 0;
            }
        }

        if (remainingBudget > 0)
        {
        }

        return totalSpawned;
    }

    /// <summary>
    /// Populates the dungeon with gold piles scattered randomly.
    /// Divides the total gold budget into reasonably similar-sized piles.
    /// </summary>
    private int PopulateGold(
        List<GridPosition> allWalkableTiles,
        int totalGoldBudget)
    {
        if (totalGoldBudget <= 0)
        {
            return 0;
        }

        int totalGoldSpawned = 0;
        int remainingGold = totalGoldBudget;
        var occupiedPositions = new HashSet<Vector2I>();

        // Mark positions occupied by creatures and items
        foreach (var gridPos in allWalkableTiles)
        {
            if (_entityManager.IsPositionOccupied(gridPos))
            {
                occupiedPositions.Add(new Vector2I(gridPos.X, gridPos.Y));
            }
        }

        // Determine pile sizes - randomize between 5-15 gold per pile
        var piles = new List<int>();
        while (remainingGold > 0)
        {
            int pileSize = Mathf.Min(remainingGold, GD.RandRange(5, 15));
            piles.Add(pileSize);
            remainingGold -= pileSize;
        }


        // Shuffle walkable tiles for random distribution
        var shuffledTiles = allWalkableTiles.OrderBy(_ => GD.Randi()).ToList();

        // Spawn each gold pile
        int pileIndex = 0;
        foreach (var pile in piles)
        {
            // Find an unoccupied tile
            Vector2I? spawnPos = null;
            foreach (var tile in shuffledTiles)
            {
                var vec = new Vector2I(tile.X, tile.Y);
                if (!occupiedPositions.Contains(vec))
                {
                    spawnPos = vec;
                    occupiedPositions.Add(vec);
                    break;
                }
            }

            if (!spawnPos.HasValue)
            {
                continue;
            }

            // Create gold entity
            var goldEntity = new Entities.Gold
            {
                Name = $"Gold_{pileIndex}" // Set node name for debugging
            };
            goldEntity.Initialize(pile, new GridPosition(spawnPos.Value.X, spawnPos.Value.Y));

            _entityManager.AddEntity(goldEntity);
            totalGoldSpawned += pile;
            pileIndex++;
        }

        return totalGoldSpawned;
    }

    /// <summary>
    /// Gets spawn table for the specified floor depth.
    /// </summary>
    private SpawnTableData GetSpawnTableForFloor(int floorDepth)
    {
        // Convert floor depth (1-based) to array index (0-based)
        // Clamp to valid range (floors 1-10 map to indices 0-9)
        int index = Mathf.Clamp(floorDepth - 1, 0, _spawnTableIdsByFloor.Length - 1);
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

    /// <summary>
    /// Spawns stairs down (floors 1-9) or Throne of Despair (floor 10).
    /// Placed far from the player to encourage exploration.
    /// </summary>
    private void SpawnStairsOrThrone(List<GridPosition> allWalkableTiles, GridPosition playerPosition)
    {
        if (allWalkableTiles == null || allWalkableTiles.Count == 0)
        {
            GD.PushWarning("SpawnStairsOrThrone: No walkable tiles available");
            return;
        }

        // Minimum distance from player (squared for Euclidean comparison)
        const int stairsMinDistanceFromPlayer = 25;
        int minDistanceSquared = stairsMinDistanceFromPlayer * stairsMinDistanceFromPlayer;

        // Get unoccupied tiles that meet minimum distance
        var farTiles = allWalkableTiles
            .Where(tile => _entityManager.GetEntityAtPosition(tile) == null)
            .Where(tile => DistanceHelper.EuclideanDistance(tile, playerPosition) >= minDistanceSquared)
            .ToList();

        GridPosition spawnPosition;
        if (farTiles.Count > 0)
        {
            // Pick any random tile that meets minimum distance
            spawnPosition = farTiles[(int)(GD.Randi() % farTiles.Count)];
        }
        else
        {
            // Fallback: no tiles meet minimum distance, use the farthest available
            var allCandidates = allWalkableTiles
                .Where(tile => _entityManager.GetEntityAtPosition(tile) == null)
                .ToList();

            if (allCandidates.Count == 0)
            {
                GD.PushWarning("SpawnStairsOrThrone: No available tiles for stairs/throne");
                return;
            }

            spawnPosition = allCandidates
                .OrderByDescending(tile => DistanceHelper.EuclideanDistance(tile, playerPosition))
                .First();
        }

        // Spawn stairs or throne based on floor depth
        if (_floorDepth < 10)
        {
            // Spawn stairs down
            var stairs = new Entities.Stairs
            {
                Name = "StairsDown"
            };
            stairs.Initialize(spawnPosition);
            _entityManager.AddEntity(stairs);
        }
        else
        {
            // Floor 10: Spawn Throne of Despair
            var throne = new Entities.ThroneOfDespair
            {
                Name = "ThroneOfDespair"
            };
            throne.Initialize(spawnPosition);
            _entityManager.AddEntity(throne);
        }
    }
}
