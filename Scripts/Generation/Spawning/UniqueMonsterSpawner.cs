using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Helpers;
using PitsOfDespair.Generation.Spawning.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Spawns unique monsters based on spawn chance rolls.
/// Tracks spawned uniques to prevent duplicates within a run.
/// </summary>
public class UniqueMonsterSpawner
{
    private const int PlayerExclusionRadius = 13;

    private readonly DataLoader _dataLoader;
    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly RandomNumberGenerator _rng;

    /// <summary>
    /// Set of unique creature IDs that have already spawned this run.
    /// </summary>
    private readonly HashSet<string> _spawnedUniques = new();

    public UniqueMonsterSpawner(
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
    /// Spawns unique monsters defined in the floor config based on spawn chance rolls.
    /// Only spawns each unique once per run.
    /// </summary>
    /// <param name="floorConfig">Floor spawn configuration</param>
    /// <param name="regions">Available regions for placement</param>
    /// <param name="metadata">Dungeon metadata</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <param name="playerPosition">Player position for exclusion zone</param>
    /// <returns>List of spawned unique creatures with their threat cost</returns>
    public List<(BaseEntity entity, int threat)> SpawnUniques(
        FloorSpawnConfig floorConfig,
        List<Region> regions,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions,
        GridPosition playerPosition)
    {
        var spawned = new List<(BaseEntity entity, int threat)>();

        if (floorConfig?.UniqueCreatures == null || floorConfig.UniqueCreatures.Count == 0)
            return spawned;

        foreach (var uniqueEntry in floorConfig.UniqueCreatures)
        {
            string creatureId = uniqueEntry.Id;

            // Skip if already spawned this run
            if (_spawnedUniques.Contains(creatureId))
            {
                GD.Print($"UniqueMonsterSpawner: Skipping {creatureId} - already spawned this run");
                continue;
            }

            // Roll for spawn chance
            float roll = _rng.Randf();
            if (roll > uniqueEntry.SpawnChance)
            {
                GD.Print($"UniqueMonsterSpawner: {creatureId} failed spawn roll ({roll:F2} > {uniqueEntry.SpawnChance:F2})");
                continue;
            }

            var creatureData = _dataLoader.Creatures.Get(creatureId);
            if (creatureData == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: Unknown creature ID '{creatureId}'");
                continue;
            }

            // Find suitable region (prefer large regions away from entrance and player)
            var region = FindLairRegion(regions, metadata, playerPosition);
            if (region == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: No suitable region for {creatureId}");
                continue;
            }

            // Find position (prefer center of region, away from player)
            var position = FindUniquePosition(region, metadata, occupiedPositions, playerPosition);
            if (position == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: No position available for {creatureId}");
                continue;
            }

            // Spawn the creature
            var entity = _entityFactory.CreateCreature(creatureId, position.Value);
            if (entity == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: Failed to create {creatureId}");
                continue;
            }

            _entityManager.AddEntity(entity);
            occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));
            _spawnedUniques.Add(creatureId);

            spawned.Add((entity, creatureData.Threat));
            GD.Print($"[UniqueMonsterSpawner] Spawned unique '{creatureData.Name}' at {position.Value} (roll {roll:F2} <= {uniqueEntry.SpawnChance:F2})");
        }

        return spawned;
    }

    /// <summary>
    /// Finds a large region suitable for a unique monster lair.
    /// Prefers regions away from the entrance and player.
    /// </summary>
    private Region FindLairRegion(List<Region> regions, DungeonMetadata metadata, GridPosition playerPosition)
    {
        if (regions == null || regions.Count == 0)
            return null;

        // Filter to large regions (at least 25 tiles)
        var largeRegions = regions.Where(r => r.Area >= 25).ToList();
        if (largeRegions.Count == 0)
            largeRegions = regions; // Fallback to any region

        // Filter out regions in player exclusion zone
        int radiusSquared = PlayerExclusionRadius * PlayerExclusionRadius;
        var validRegions = largeRegions.Where(r =>
        {
            int distSquared = DistanceHelper.EuclideanDistance(r.Centroid, playerPosition);
            return distSquared >= radiusSquared;
        }).ToList();

        // If all regions filtered out, fall back to large regions
        if (validRegions.Count == 0)
        {
            GD.PushWarning("UniqueMonsterSpawner: All regions in player exclusion zone, using any large region");
            validRegions = largeRegions;
        }

        // If we have entrance distance, prefer regions far from entrance
        if (metadata?.EntranceDistance != null)
        {
            return validRegions
                .OrderByDescending(r => metadata.EntranceDistance.GetDistance(r.Centroid))
                .FirstOrDefault();
        }

        // Otherwise prefer the largest region
        return validRegions.OrderByDescending(r => r.Area).FirstOrDefault();
    }

    /// <summary>
    /// Finds a position for a unique monster, preferring the center of the region.
    /// Excludes positions too close to the player.
    /// </summary>
    private GridPosition? FindUniquePosition(
        Region region,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions,
        GridPosition playerPosition)
    {
        int radiusSquared = PlayerExclusionRadius * PlayerExclusionRadius;

        // Filter tiles outside player exclusion zone
        var validTiles = region.Tiles
            .Where(t =>
            {
                if (occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
                    return false;
                int distSquared = DistanceHelper.EuclideanDistance(t, playerPosition);
                return distSquared >= radiusSquared;
            })
            .ToList();

        if (validTiles.Count == 0)
        {
            GD.PushWarning("UniqueMonsterSpawner: No valid positions outside player exclusion zone");
            return null;
        }

        // Try centroid first if valid
        var centroid = region.Centroid;
        if (validTiles.Contains(centroid))
            return centroid;

        // Try tiles near centroid
        return validTiles
            .OrderBy(t => Mathf.Abs(t.X - centroid.X) + Mathf.Abs(t.Y - centroid.Y))
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a unique creature has already been spawned this run.
    /// </summary>
    public bool HasSpawned(string creatureId)
    {
        return _spawnedUniques.Contains(creatureId);
    }

    /// <summary>
    /// Marks a unique as spawned (for external tracking).
    /// </summary>
    public void MarkSpawned(string creatureId)
    {
        _spawnedUniques.Add(creatureId);
    }

    /// <summary>
    /// Resets the spawned uniques tracker for a new run.
    /// </summary>
    public void ResetForNewRun()
    {
        _spawnedUniques.Clear();
    }

    /// <summary>
    /// Gets the set of uniques that have been spawned this run.
    /// </summary>
    public IReadOnlyCollection<string> GetSpawnedUniques()
    {
        return _spawnedUniques;
    }
}
