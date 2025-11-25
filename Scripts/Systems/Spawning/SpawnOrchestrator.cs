using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Main orchestrator for dungeon population.
/// Coordinates all spawning phases in the correct order using the power-budget system.
/// Replaces the old SpawnManager with encounter-based, region-themed spawning.
/// </summary>
public partial class SpawnOrchestrator : Node
{
    private EntityFactory _entityFactory;
    private EntityManager _entityManager;
    private MapSystem _mapSystem;
    private DataLoader _dataLoader;
    private int _floorDepth;

    // Sub-systems
    private RegionThemeAssigner _themeAssigner;
    private RegionBudgetAllocator _budgetAllocator;
    private EncounterPlacer _encounterPlacer;
    private EncounterSpawner _encounterSpawner;
    private SpawnAIConfigurator _aiConfigurator;
    private TreasurePlacer _treasurePlacer;
    private LootDistributor _lootDistributor;
    private GoldPlacer _goldPlacer;
    private StairsSpawner _stairsSpawner;
    private UniqueMonsterSpawner _uniqueSpawner;
    private OutOfDepthSpawner _outOfDepthSpawner;

    // Configuration
    private const int PlayerExclusionRadius = 13;
    private const int MinimumCreatureCount = 3;

    /// <summary>
    /// Last spawn summary for debug inspection.
    /// </summary>
    public SpawnSummary LastSpawnSummary { get; private set; }

    /// <summary>
    /// Sets dependencies for the orchestrator.
    /// Must be called before PopulateFloor.
    /// </summary>
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

        InitializeSubSystems();
    }

    /// <summary>
    /// Initializes all spawning subsystems.
    /// </summary>
    private void InitializeSubSystems()
    {
        _themeAssigner = new RegionThemeAssigner(_dataLoader);
        _budgetAllocator = new RegionBudgetAllocator();
        _encounterPlacer = new EncounterPlacer(_dataLoader);
        _encounterSpawner = new EncounterSpawner(_entityFactory, _entityManager, _dataLoader);
        _aiConfigurator = new SpawnAIConfigurator();
        _treasurePlacer = new TreasurePlacer(_dataLoader, _entityFactory, _entityManager);
        _lootDistributor = new LootDistributor(_dataLoader, _entityFactory, _entityManager);
        _goldPlacer = new GoldPlacer(_entityManager);
        _stairsSpawner = new StairsSpawner(_entityManager);
        _uniqueSpawner = new UniqueMonsterSpawner(_dataLoader, _entityFactory, _entityManager);
        _outOfDepthSpawner = new OutOfDepthSpawner(_dataLoader, _entityFactory, _entityManager);
    }

    /// <summary>
    /// Main entry point: Populates the floor with creatures, items, and features.
    /// Coordinates all spawning phases in order.
    /// </summary>
    /// <param name="playerPosition">Player's starting position for exclusion zone</param>
    /// <returns>Spawn summary with statistics and debug info</returns>
    public SpawnSummary PopulateFloor(GridPosition playerPosition)
    {
        var stopwatch = Stopwatch.StartNew();
        var summary = new SpawnSummary { FloorDepth = _floorDepth };

        // Phase 1: Load floor spawn config
        var floorConfig = _dataLoader.GetFloorSpawnConfigForDepth(_floorDepth);
        if (floorConfig == null)
        {
            summary.AddWarning($"No spawn config found for floor {_floorDepth}, using fallback");
            floorConfig = CreateFallbackConfig();
        }

        // Get dungeon metadata
        var metadata = _mapSystem.Metadata;
        if (metadata == null)
        {
            summary.AddWarning("No dungeon metadata available");
            stopwatch.Stop();
            summary.SpawnTimeMs = stopwatch.ElapsedMilliseconds;
            LastSpawnSummary = summary;
            return summary;
        }

        // Set entrance position for distance calculations
        metadata.EntrancePosition = playerPosition;

        // Track occupied positions across all phases
        var occupiedPositions = new HashSet<Vector2I>();

        // Add player position to exclusion zone
        occupiedPositions.Add(new Vector2I(playerPosition.X, playerPosition.Y));

        // Region spawn data tracking
        var regionSpawnData = new Dictionary<int, RegionSpawnData>();

        // Roll budgets
        int powerBudget = floorConfig.RollPowerBudget();
        int itemBudget = floorConfig.RollItemBudget();
        int goldBudget = floorConfig.RollGoldBudget();

        summary.TotalPowerBudget = powerBudget;
        summary.TotalItemBudget = itemBudget;
        summary.TotalGoldBudget = goldBudget;

        // Phase 2: Assign themes to regions
        _themeAssigner.AssignThemes(metadata, floorConfig, regionSpawnData);

        // Phase 3: Allocate budgets to regions
        _budgetAllocator.AllocateBudgets(metadata, powerBudget, regionSpawnData, playerPosition);
        _budgetAllocator.CalculateDangerLevels(metadata, regionSpawnData, playerPosition);

        // Record theme distribution
        foreach (var (regionId, spawnData) in regionSpawnData)
        {
            if (spawnData.Theme != null)
            {
                summary.RecordTheme(spawnData.Theme.Id);
            }
        }

        // Phase 4: Process prefab SpawnHints (deduct from region budgets)
        ProcessPrefabSpawnHints(metadata, floorConfig, regionSpawnData, occupiedPositions, summary);

        // Phase 5: Spawn unique monsters
        var uniqueSpawns = _uniqueSpawner.SpawnUniques(
            floorConfig,
            metadata.Regions,
            metadata,
            occupiedPositions);

        foreach (var (entity, threat) in uniqueSpawns)
        {
            summary.UniqueSpawns.Add(entity.Name);
            summary.TotalThreatSpawned += threat;
        }

        // Phase 6: Spawn encounters per region
        var allEncounters = new List<SpawnedEncounter>();
        foreach (var region in metadata.Regions)
        {
            if (!regionSpawnData.TryGetValue(region.Id, out var spawnData))
                continue;

            // Skip regions in player exclusion zone
            if (IsRegionInExclusionZone(region, playerPosition))
                continue;

            // Place encounters in this region
            var encounters = _encounterPlacer.PlaceEncountersInRegion(
                region, spawnData, floorConfig, occupiedPositions);

            // Spawn creatures for each encounter
            foreach (var encounter in encounters)
            {
                bool success = _encounterSpawner.SpawnEncounter(encounter, occupiedPositions);
                if (success)
                {
                    allEncounters.Add(encounter);
                    summary.EncountersPlaced++;
                    summary.CreaturesSpawned += encounter.CreatureCount;
                    summary.TotalThreatSpawned += encounter.TotalThreat;

                    if (encounter.Template != null)
                    {
                        summary.RecordEncounter(encounter.Template.Id);
                    }
                }
            }

            summary.RegionsProcessed++;
        }

        // Phase 7: Place treasures and items
        int itemsPlaced = PlaceItemsAndTreasure(
            metadata, floorConfig, regionSpawnData, itemBudget, occupiedPositions);
        summary.ItemsPlaced = itemsPlaced;

        // Phase 8: Place gold
        int goldPlaced = _goldPlacer.DistributeGold(
            metadata.Regions,
            regionSpawnData,
            goldBudget,
            _floorDepth,
            occupiedPositions);
        summary.GoldPlaced = goldPlaced;

        // Phase 9: Place stairs/throne
        var stairsPosition = _stairsSpawner.PlaceStairs(metadata, _floorDepth, occupiedPositions);
        if (stairsPosition.HasValue)
        {
            summary.StairsPosition = $"({stairsPosition.Value.X}, {stairsPosition.Value.Y})";
        }
        else
        {
            summary.AddWarning("Failed to place stairs");
        }

        // Phase 10: Configure AI for all spawned encounters
        foreach (var encounter in allEncounters)
        {
            _aiConfigurator.ConfigureEncounter(encounter);
        }

        // Phase 11: Out-of-depth spawn (if triggered)
        TrySpawnOutOfDepth(floorConfig, metadata, regionSpawnData, occupiedPositions, summary);

        // Validation
        ValidateSpawning(summary, metadata, playerPosition);

        stopwatch.Stop();
        summary.SpawnTimeMs = stopwatch.ElapsedMilliseconds;

        // Log summary
        GD.Print(summary.ToDebugString());

        LastSpawnSummary = summary;
        return summary;
    }

    /// <summary>
    /// Processes prefab spawn hints before regular encounter spawning.
    /// </summary>
    private void ProcessPrefabSpawnHints(
        DungeonMetadata metadata,
        FloorSpawnConfig floorConfig,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        HashSet<Vector2I> occupiedPositions,
        SpawnSummary summary)
    {
        var spawnableRegions = metadata.GetSpawnableRegions();
        if (spawnableRegions.Count == 0)
            return;

        foreach (var region in spawnableRegions)
        {
            if (!regionSpawnData.TryGetValue(region.Id, out var spawnData))
                continue;

            foreach (var hint in region.SpawnHints)
            {
                // Handle encounter template hints
                if (!string.IsNullOrEmpty(hint.EncounterTemplateId))
                {
                    var template = _dataLoader.GetEncounterTemplate(hint.EncounterTemplateId);
                    if (template != null && spawnData.Theme != null)
                    {
                        var encounter = new SpawnedEncounter
                        {
                            Template = template,
                            Theme = spawnData.Theme,
                            Region = region,
                            CenterPosition = hint.Position ?? region.Centroid,
                            TotalThreat = template.MinBudget
                        };

                        if (_encounterSpawner.SpawnEncounter(encounter, occupiedPositions))
                        {
                            spawnData.ConsumeBudget(encounter.TotalThreat);
                            summary.EncountersPlaced++;
                            summary.CreaturesSpawned += encounter.CreatureCount;
                            summary.TotalThreatSpawned += encounter.TotalThreat;
                        }
                    }
                }
                // Handle direct creature spawns
                else if (hint.CreaturePool != null && hint.CreaturePool.Count > 0)
                {
                    var creatureId = hint.CreaturePool[GD.RandRange(0, hint.CreaturePool.Count - 1)];
                    var creatureData = _dataLoader.GetCreature(creatureId);
                    if (creatureData == null)
                        continue;

                    var position = hint.Position ?? FindAvailablePosition(region, occupiedPositions);
                    if (position == null)
                        continue;

                    var entity = _entityFactory.CreateCreature(creatureId, position.Value);
                    if (entity != null)
                    {
                        _entityManager.AddEntity(entity);
                        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                        spawnData.ConsumeBudget(creatureData.Threat);
                        summary.CreaturesSpawned++;
                        summary.TotalThreatSpawned += creatureData.Threat;
                    }
                }
                // Handle item spawns
                else if (!string.IsNullOrEmpty(hint.ItemId))
                {
                    var position = hint.Position ?? FindAvailablePosition(region, occupiedPositions);
                    if (position == null)
                        continue;

                    var itemEntity = _entityFactory.CreateItem(hint.ItemId, position.Value);
                    if (itemEntity != null)
                    {
                        _entityManager.AddEntity(itemEntity);
                        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                        summary.ItemsPlaced++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places items and treasure based on region danger and budget.
    /// </summary>
    private int PlaceItemsAndTreasure(
        DungeonMetadata metadata,
        FloorSpawnConfig floorConfig,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        int itemBudget,
        HashSet<Vector2I> occupiedPositions)
    {
        int totalPlaced = 0;

        // Place guarded treasure in dangerous regions (risk = reward)
        var dangerousRegions = metadata.Regions
            .Where(r => regionSpawnData.ContainsKey(r.Id) &&
                       regionSpawnData[r.Id].TotalThreatSpawned > 5)
            .OrderByDescending(r => regionSpawnData[r.Id].TotalThreatSpawned)
            .Take(3)
            .ToList();

        foreach (var region in dangerousRegions)
        {
            var spawnData = regionSpawnData[region.Id];
            int guardianThreat = spawnData.TotalThreatSpawned / 2;

            int cost = _treasurePlacer.PlaceGuardedTreasure(
                region, floorConfig.Items, itemBudget, guardianThreat, occupiedPositions);

            if (cost > 0)
            {
                totalPlaced++;
                itemBudget -= cost;
            }
        }

        // Distribute remaining items across all regions
        if (itemBudget > 0)
        {
            int distributed = _lootDistributor.DistributeItems(
                metadata.Regions,
                regionSpawnData,
                floorConfig.Items,
                itemBudget,
                occupiedPositions);

            totalPlaced += distributed;
        }

        return totalPlaced;
    }

    /// <summary>
    /// Attempts to spawn an out-of-depth creature.
    /// </summary>
    private void TrySpawnOutOfDepth(
        FloorSpawnConfig floorConfig,
        DungeonMetadata metadata,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        HashSet<Vector2I> occupiedPositions,
        SpawnSummary summary)
    {
        // Get configs for deeper floors
        var deeperConfigs = new List<FloorSpawnConfig>();
        for (int i = 1; i <= floorConfig.OutOfDepthFloors; i++)
        {
            var deeper = _dataLoader.GetFloorSpawnConfigForDepth(_floorDepth + i);
            if (deeper != null)
            {
                deeperConfigs.Add(deeper);
            }
        }

        var result = _outOfDepthSpawner.TrySpawnOutOfDepth(
            _floorDepth,
            floorConfig,
            deeperConfigs,
            metadata.Regions,
            metadata,
            occupiedPositions);

        if (result.HasValue)
        {
            var (entity, threat) = result.Value;
            summary.OutOfDepthSpawn = $"{entity.Name} (threat {threat})";
            summary.TotalThreatSpawned += threat;
            summary.CreaturesSpawned++;
        }
    }

    /// <summary>
    /// Validates the spawning results.
    /// </summary>
    private void ValidateSpawning(SpawnSummary summary, DungeonMetadata metadata, GridPosition playerPosition)
    {
        // Check minimum creature count
        if (summary.CreaturesSpawned < MinimumCreatureCount)
        {
            summary.AddWarning($"Below minimum creature count: {summary.CreaturesSpawned}/{MinimumCreatureCount}");
        }

        // Check stairs were placed
        if (string.IsNullOrEmpty(summary.StairsPosition))
        {
            summary.AddWarning("Stairs/throne not placed - player cannot progress");
        }

        // Check player can reach stairs (basic validation)
        if (metadata.ExitPosition.HasValue && metadata.EntranceDistance != null)
        {
            float distanceToExit = metadata.EntranceDistance.GetDistance(metadata.ExitPosition.Value);
            if (distanceToExit < 0 || distanceToExit >= float.MaxValue)
            {
                summary.AddWarning("Stairs may be unreachable from entrance");
            }
        }

        // Check budget utilization
        if (summary.PowerBudgetUtilization < 50f)
        {
            summary.AddWarning($"Low power budget utilization: {summary.PowerBudgetUtilization:F1}%");
        }
    }

    /// <summary>
    /// Checks if a region is within the player exclusion zone.
    /// </summary>
    private bool IsRegionInExclusionZone(Region region, GridPosition playerPosition)
    {
        int radiusSquared = PlayerExclusionRadius * PlayerExclusionRadius;
        int distSquared = DistanceHelper.EuclideanDistance(region.Centroid, playerPosition);
        return distSquared < radiusSquared;
    }

    /// <summary>
    /// Finds an available position in a region.
    /// </summary>
    private GridPosition? FindAvailablePosition(Region region, HashSet<Vector2I> occupiedPositions)
    {
        foreach (var tile in region.Tiles)
        {
            var vec = new Vector2I(tile.X, tile.Y);
            if (!occupiedPositions.Contains(vec))
            {
                return tile;
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a minimal fallback config when no YAML config exists.
    /// </summary>
    private FloorSpawnConfig CreateFallbackConfig()
    {
        return new FloorSpawnConfig
        {
            Id = $"fallback_floor_{_floorDepth}",
            Name = $"Fallback Floor {_floorDepth}",
            MinFloor = _floorDepth,
            MaxFloor = _floorDepth,
            PowerBudget = $"{_floorDepth + 2}d4+{_floorDepth * 2}",
            ItemBudget = "1d4+1",
            GoldBudget = $"{_floorDepth}d10+10",
            MinThreat = 1,
            MaxThreat = _floorDepth * 3 + 5
        };
    }

    /// <summary>
    /// Resets unique monster tracking for a new run.
    /// Call this when starting a new game.
    /// </summary>
    public void ResetForNewRun()
    {
        _uniqueSpawner?.ResetForNewRun();
    }

    /// <summary>
    /// Gets the unique monster spawner for external tracking.
    /// </summary>
    public UniqueMonsterSpawner GetUniqueSpawner()
    {
        return _uniqueSpawner;
    }
}
