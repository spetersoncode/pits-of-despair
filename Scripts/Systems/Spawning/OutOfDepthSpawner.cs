using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Spawns out-of-depth creatures from deeper floors.
/// Creates memorable danger moments with elite/mini-boss encounters.
/// </summary>
public class OutOfDepthSpawner
{
    private readonly DataLoader _dataLoader;
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly RandomNumberGenerator _rng;

    public OutOfDepthSpawner(
        DataLoader dataLoader,
        EntityFactory entityFactory,
        EntityManager entityManager)
    {
        _dataLoader = dataLoader;
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    /// <summary>
    /// Attempts to spawn an out-of-depth creature based on floor config.
    /// </summary>
    /// <param name="currentFloor">Current floor depth</param>
    /// <param name="floorConfig">Current floor's spawn config</param>
    /// <param name="deeperConfigs">Floor configs for deeper floors</param>
    /// <param name="regions">Available regions</param>
    /// <param name="metadata">Dungeon metadata</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>Spawned entity and threat, or null if not triggered/failed</returns>
    public (BaseEntity entity, int threat)? TrySpawnOutOfDepth(
        int currentFloor,
        FloorSpawnConfig floorConfig,
        List<FloorSpawnConfig> deeperConfigs,
        List<Region> regions,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions)
    {
        // Check if out-of-depth triggers
        if (floorConfig.CreatureOutOfDepthChance <= 0)
            return null;

        float roll = _rng.Randf();
        if (roll > floorConfig.CreatureOutOfDepthChance)
            return null;

        GD.Print($"OutOfDepthSpawner: Out-of-depth triggered! (rolled {roll:F2} vs {floorConfig.CreatureOutOfDepthChance:F2})");

        // Find valid deeper floor config
        int targetFloor = currentFloor + floorConfig.OutOfDepthFloors;
        var deeperConfig = deeperConfigs?
            .FirstOrDefault(c => c.MinFloor <= targetFloor && c.MaxFloor >= targetFloor);

        if (deeperConfig == null)
        {
            GD.Print("OutOfDepthSpawner: No deeper floor config available");
            return null;
        }

        // Select a creature from the deeper floor's theme pools
        var (creatureId, creatureData) = SelectDeeperCreature(deeperConfig, floorConfig);
        if (creatureData == null)
        {
            GD.Print("OutOfDepthSpawner: No suitable creature found from deeper floor");
            return null;
        }

        // Find a region for placement (prefer interesting locations)
        var region = FindDangerousRegion(regions, metadata);
        if (region == null)
        {
            GD.Print("OutOfDepthSpawner: No suitable region for out-of-depth spawn");
            return null;
        }

        var position = FindOutOfDepthPosition(region, metadata, occupiedPositions);
        if (position == null)
        {
            GD.Print("OutOfDepthSpawner: No available position");
            return null;
        }

        // Spawn the creature
        var entity = _entityFactory.CreateCreature(creatureId, position.Value);
        if (entity == null)
        {
            GD.Print($"OutOfDepthSpawner: Failed to create creature {creatureId}");
            return null;
        }

        _entityManager.AddEntity(entity);
        occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));

        GD.Print($"OutOfDepthSpawner: Spawned '{creatureData.Name}' (threat {creatureData.Threat}) " +
                 $"from floor {targetFloor} at {position.Value}");

        return (entity, creatureData.Threat);
    }

    /// <summary>
    /// Selects a creature from the deeper floor's available themes.
    /// Prefers creatures above the current floor's max threat.
    /// </summary>
    private (string id, CreatureData data) SelectDeeperCreature(
        FloorSpawnConfig deeperConfig,
        FloorSpawnConfig currentConfig)
    {
        var candidates = new List<(string id, CreatureData data)>();

        // Get creatures from deeper floor's themes
        foreach (var themeEntry in deeperConfig.ThemeWeights)
        {
            var theme = _dataLoader.Spawning.GetFactionTheme(themeEntry.Id);
            if (theme?.Creatures == null)
                continue;

            foreach (var creatureId in theme.Creatures)
            {
                var data = _dataLoader.Creatures.Get(creatureId);
                if (data == null)
                    continue;

                // Prefer creatures above current floor's max threat (truly dangerous)
                if (data.Threat > currentConfig.MaxThreat)
                {
                    candidates.Add((creatureId, data));
                }
            }
        }

        // If no creatures above max threat, take any from deeper themes
        if (candidates.Count == 0)
        {
            foreach (var themeEntry in deeperConfig.ThemeWeights)
            {
                var theme = _dataLoader.Spawning.GetFactionTheme(themeEntry.Id);
                if (theme?.Creatures == null)
                    continue;

                foreach (var creatureId in theme.Creatures)
                {
                    var data = _dataLoader.Creatures.Get(creatureId);
                    if (data != null && data.Threat >= deeperConfig.MinThreat)
                    {
                        candidates.Add((creatureId, data));
                    }
                }
            }
        }

        if (candidates.Count == 0)
            return (null, null);

        // Prefer higher threat creatures (more memorable)
        var sorted = candidates.OrderByDescending(c => c.data.Threat).ToList();

        // Pick from top third (most dangerous)
        int pickRange = Mathf.Max(1, sorted.Count / 3);
        var selected = sorted[_rng.RandiRange(0, pickRange - 1)];
        return selected;
    }

    /// <summary>
    /// Finds a region suitable for a dangerous out-of-depth spawn.
    /// Prefers chokepoints and mid-dungeon areas.
    /// </summary>
    private Region FindDangerousRegion(List<Region> regions, DungeonMetadata metadata)
    {
        if (regions == null || regions.Count == 0)
            return null;

        // Prefer regions in the middle of the dungeon (not too close to entrance or exit)
        if (metadata?.EntranceDistance != null)
        {
            var sorted = regions
                .Select(r => new
                {
                    Region = r,
                    Distance = metadata.EntranceDistance.GetDistance(r.Centroid)
                })
                .Where(x => x.Distance > 0 && x.Distance < float.MaxValue)
                .OrderBy(x => x.Distance)
                .ToList();

            if (sorted.Count >= 3)
            {
                // Pick from middle third
                int start = sorted.Count / 3;
                int end = start * 2;
                return sorted[_rng.RandiRange(start, end)].Region;
            }
        }

        // Fallback: random region
        return regions[_rng.RandiRange(0, regions.Count - 1)];
    }

    /// <summary>
    /// Finds a position for the out-of-depth creature.
    /// Prefers strategic positions like chokepoints or near passages.
    /// </summary>
    private GridPosition? FindOutOfDepthPosition(
        Region region,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions)
    {
        // Try edge tiles first (creates ambush feel)
        var edgeTiles = region.EdgeTiles?
            .Where(t => !occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
            .ToList();

        if (edgeTiles != null && edgeTiles.Count > 0)
        {
            return edgeTiles[_rng.RandiRange(0, edgeTiles.Count - 1)];
        }

        // Fallback to any tile
        var available = region.Tiles
            .Where(t => !occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
            .ToList();

        if (available.Count == 0)
            return null;

        return available[_rng.RandiRange(0, available.Count - 1)];
    }
}
