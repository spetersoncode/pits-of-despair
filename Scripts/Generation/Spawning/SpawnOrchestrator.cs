using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;
using PitsOfDespair.Generation.Spawning.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Main orchestrator for dungeon population.
/// Coordinates all spawning phases in the correct order using density-driven spawning.
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
    private RegionDangerCalculator _dangerCalculator;
    private EncounterPlacer _encounterPlacer;
    private EncounterSpawner _encounterSpawner;
    private SpawnAIConfigurator _aiConfigurator;
    private TreasurePlacer _treasurePlacer;
    private LootDistributor _lootDistributor;
    private GoldPlacer _goldPlacer;
    private StairsSpawner _stairsSpawner;
    private UniqueMonsterSpawner _uniqueSpawner;
    private OutOfDepthSpawner _outOfDepthSpawner;
    private DecorationSpawner _decorationSpawner;

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
        _dangerCalculator = new RegionDangerCalculator();
        _encounterPlacer = new EncounterPlacer(_dataLoader);
        _encounterSpawner = new EncounterSpawner(_entityFactory, _entityManager, _dataLoader);
        _aiConfigurator = new SpawnAIConfigurator(_mapSystem);
        _treasurePlacer = new TreasurePlacer(_dataLoader, _entityFactory, _entityManager);
        _lootDistributor = new LootDistributor(_dataLoader, _entityFactory, _entityManager);
        _goldPlacer = new GoldPlacer(_entityManager);
        _stairsSpawner = new StairsSpawner(_entityManager);
        _uniqueSpawner = new UniqueMonsterSpawner(_dataLoader, _entityFactory, _entityManager);
        _outOfDepthSpawner = new OutOfDepthSpawner(_dataLoader, _entityFactory, _entityManager);
        _decorationSpawner = new DecorationSpawner(_dataLoader, _entityFactory, _entityManager, _mapSystem);
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

        // Phase 1: Load spawn config from Pipeline + Floor configs
        var spawnConfig = GetSpawnConfig();
        if (spawnConfig == null)
        {
            GD.PushError($"[SpawnOrchestrator] FATAL: No spawn config for floor {_floorDepth} - check Pipeline and Floor configs!");
            summary.AddWarning($"FATAL: No spawn config found for floor {_floorDepth}");
            stopwatch.Stop();
            summary.SpawnTimeMs = stopwatch.ElapsedMilliseconds;
            LastSpawnSummary = summary;
            return summary;
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

        // Phase 2: Assign themes to regions
        _themeAssigner.AssignThemes(metadata, spawnConfig, regionSpawnData);

        // Phase 3: Calculate danger levels for regions
        _dangerCalculator.CalculateDangerLevels(metadata, regionSpawnData, playerPosition);

        // Record theme distribution
        foreach (var (regionId, spawnData) in regionSpawnData)
        {
            if (spawnData.Theme != null)
            {
                summary.RecordTheme(spawnData.Theme.Id);
            }
        }

        // Phase 4: Process prefab SpawnHints (deduct from region budgets)
        ProcessPrefabSpawnHints(metadata, spawnConfig, regionSpawnData, occupiedPositions, summary);

        // Phase 5: Spawn unique monsters
        var uniqueSpawns = _uniqueSpawner.SpawnUniques(
            spawnConfig,
            metadata.Regions,
            metadata,
            occupiedPositions,
            playerPosition);

        foreach (var (entity, threat) in uniqueSpawns)
        {
            summary.UniqueSpawns.Add(entity.Name);
            summary.TotalThreatSpawned += threat;
        }

        // Phase 6: Spawn encounters per region
        var allEncounters = new List<SpawnedEncounter>();
        int maxEncounters = Mathf.RoundToInt(metadata.Regions.Count * spawnConfig.MaxEncounterRatio);

        foreach (var region in metadata.Regions)
        {
            // Stop if we've hit the encounter cap
            if (allEncounters.Count >= maxEncounters)
                break;

            if (!regionSpawnData.TryGetValue(region.Id, out var spawnData))
                continue;

            // Skip regions in player exclusion zone
            if (IsRegionInExclusionZone(region, playerPosition, spawnConfig))
                continue;

            // Place encounters in this region
            var encounters = _encounterPlacer.PlaceEncountersInRegion(
                region, spawnData, spawnConfig, occupiedPositions);

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

        // Phase 7: Place treasures and items (density-based)
        var (itemsPlaced, itemValue) = PlaceItemsAndTreasure(
            metadata, spawnConfig, regionSpawnData, occupiedPositions);
        summary.ItemsPlaced = itemsPlaced;
        summary.TotalItemValue = itemValue;

        // Phase 8: Place gold (floor-based formula)
        int goldPlaced = _goldPlacer.DistributeGold(
            metadata.Regions,
            regionSpawnData,
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
        TrySpawnOutOfDepth(spawnConfig, metadata, regionSpawnData, occupiedPositions, summary);

        // Phase 12: Place decorations
        // IMPORTANT: This must run after AI configuration (Phase 10) because patrol routes
        // select waypoints from walkable tiles. If blocking decorations were placed before
        // patrol generation, patrol waypoints could end up on blocked positions.
        int decorationsPlaced = _decorationSpawner.SpawnDecorations(
            metadata.Regions,
            regionSpawnData,
            occupiedPositions);
        summary.DecorationsPlaced = decorationsPlaced;

        // Validation
        ValidateSpawning(summary, metadata, playerPosition, spawnConfig);

        stopwatch.Stop();
        summary.SpawnTimeMs = stopwatch.ElapsedMilliseconds;

        // Log summary
        foreach (var line in summary.GetDebugLines())
            GD.Print(line);

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
                    var template = _dataLoader.Spawning.GetEncounterTemplate(hint.EncounterTemplateId);
                    if (template != null && spawnData.Theme != null)
                    {
                        var encounter = new SpawnedEncounter
                        {
                            Template = template,
                            Theme = spawnData.Theme,
                            Region = region,
                            FloorConfig = floorConfig,
                            CenterPosition = hint.Position ?? region.Centroid,
                            TotalThreat = template.MinBudget
                        };

                        if (_encounterSpawner.SpawnEncounter(encounter, occupiedPositions))
                        {
                            spawnData.TotalThreatSpawned += encounter.TotalThreat;
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
                    var creatureData = _dataLoader.Creatures.Get(creatureId);
                    if (creatureData == null)
                    {
                        GD.PushWarning($"[SpawnOrchestrator] Prefab hint creature '{creatureId}' not found");
                        continue;
                    }

                    // Check threat band
                    if (creatureData.Threat < floorConfig.MinThreat || creatureData.Threat > floorConfig.MaxThreat)
                        continue;

                    var position = hint.Position ?? FindAvailablePosition(region, occupiedPositions);
                    if (position == null)
                        continue;

                    var entity = _entityFactory.CreateCreature(creatureId, position.Value);
                    if (entity != null)
                    {
                        _entityManager.AddEntity(entity);
                        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
                        spawnData.TotalThreatSpawned += creatureData.Threat;
                        summary.CreaturesSpawned++;
                        summary.TotalThreatSpawned += creatureData.Threat;
                    }
                    else
                    {
                        GD.PushWarning($"[SpawnOrchestrator] Failed to create prefab hint creature '{creatureId}'");
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
                    else
                    {
                        GD.PushWarning($"[SpawnOrchestrator] Failed to create prefab hint item '{hint.ItemId}'");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places items and treasure based on region danger and density.
    /// </summary>
    private (int count, int value) PlaceItemsAndTreasure(
        DungeonMetadata metadata,
        FloorSpawnConfig floorConfig,
        Dictionary<int, RegionSpawnData> regionSpawnData,
        HashSet<Vector2I> occupiedPositions)
    {
        int totalPlaced = 0;
        int totalValue = 0;

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

            var (placed, value) = _treasurePlacer.PlaceGuardedTreasure(
                region, floorConfig, guardianThreat, occupiedPositions);

            totalPlaced += placed;
            totalValue += value;
        }

        // Distribute items across all regions using density-based spawning
        var (distributed, distributedValue) = _lootDistributor.DistributeItemsByDensity(
            metadata.Regions,
            regionSpawnData,
            floorConfig,
            occupiedPositions);

        totalPlaced += distributed;
        totalValue += distributedValue;

        return (totalPlaced, totalValue);
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
        // Get configs for deeper floors from FloorConfig system
        var deeperConfigs = new List<FloorSpawnConfig>();
        for (int i = 1; i <= floorConfig.OutOfDepthFloors; i++)
        {
            var deeperFloorConfig = _dataLoader.FloorConfigs.GetForDepth(_floorDepth + i);
            if (deeperFloorConfig != null)
            {
                // Create minimal FloorSpawnConfig with fields needed by OutOfDepthSpawner
                deeperConfigs.Add(new FloorSpawnConfig
                {
                    Floor = deeperFloorConfig.Floor ?? (_floorDepth + i),
                    MinThreat = deeperFloorConfig.MinThreat,
                    MaxThreat = deeperFloorConfig.MaxThreat,
                    ThemeWeights = deeperFloorConfig.ThemeWeights
                });
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
    private void ValidateSpawning(SpawnSummary summary, DungeonMetadata metadata, GridPosition playerPosition, FloorSpawnConfig config)
    {
        // Check minimum creature count
        int minCreatures = config?.MinCreatureCount ?? 3;
        if (summary.CreaturesSpawned < minCreatures)
        {
            summary.AddWarning($"Below minimum creature count: {summary.CreaturesSpawned}/{minCreatures}");
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

    }

    /// <summary>
    /// Checks if a region is within the player exclusion zone.
    /// </summary>
    private bool IsRegionInExclusionZone(Region region, GridPosition playerPosition, FloorSpawnConfig config)
    {
        int radius = config?.PlayerExclusionRadius ?? 13;
        int radiusSquared = radius * radius;
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
    /// Gets spawn configuration by merging Pipeline and Floor configs.
    /// </summary>
    private FloorSpawnConfig GetSpawnConfig()
    {
        var pipelineConfig = _mapSystem.CurrentPipelineConfig;
        var floorConfig = _mapSystem.CurrentFloorConfig;

        if (pipelineConfig == null || floorConfig == null)
        {
            GD.PushError($"[SpawnOrchestrator] Missing pipeline or floor config for depth {_floorDepth}");
            return null;
        }

        var context = SpawnContext.Create(pipelineConfig, floorConfig);
        return ConvertToFloorSpawnConfig(context);
    }

    /// <summary>
    /// Converts a SpawnContext to FloorSpawnConfig for use by spawning subsystems.
    /// </summary>
    private FloorSpawnConfig ConvertToFloorSpawnConfig(SpawnContext context)
    {
        return new FloorSpawnConfig
        {
            Id = $"floor_{_floorDepth}",
            Name = $"Floor {_floorDepth}",
            Floor = _floorDepth,

            // From Pipeline (layout-dependent)
            ItemDensity = context.ItemDensity,
            EncounterChance = context.EncounterChance,
            MaxEncounterRatio = context.MaxEncounterRatio,
            MinEncounterSpacing = context.MinEncounterSpacing,
            MaxEncountersPerRegion = context.MaxEncountersPerRegion,
            PlayerExclusionRadius = context.PlayerExclusionRadius,
            RegionMatchMultiplier = context.RegionMatchMultiplier,
            DangerBonusMultiplier = context.DangerBonusMultiplier,
            MinCreatureCount = context.MinCreatureCount,
            EncounterWeights = context.EncounterWeights,

            // From Floor (difficulty/content)
            MinThreat = context.MinThreat,
            MaxThreat = context.MaxThreat,
            MinItemValue = context.MinItemValue,
            MaxItemValue = context.MaxItemValue,
            CreatureOutOfDepthChance = context.CreatureOutOfDepthChance,
            OutOfDepthFloors = context.OutOfDepthFloors,
            ThemeWeights = context.ThemeWeights,
            UniqueCreatures = context.UniqueCreatures,

            // Creature selection
            BaseScore = context.BaseScore,
            NoMatchScore = context.NoMatchScore,
            MatchScoreBonus = context.MatchScoreBonus,
            RoleKeywordBonus = context.RoleKeywordBonus
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
