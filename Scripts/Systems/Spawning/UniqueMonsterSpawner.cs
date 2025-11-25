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
/// Spawns unique monsters that are guaranteed to appear on specific floors.
/// Tracks spawned uniques to prevent duplicates within a run.
/// </summary>
public class UniqueMonsterSpawner
{
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
    /// Spawns unique monsters defined in the floor config.
    /// Only spawns each unique once per run.
    /// </summary>
    /// <param name="floorConfig">Floor spawn configuration</param>
    /// <param name="regions">Available regions for placement</param>
    /// <param name="metadata">Dungeon metadata</param>
    /// <param name="occupiedPositions">Already occupied positions</param>
    /// <returns>List of spawned unique creatures with their threat cost</returns>
    public List<(BaseEntity entity, int threat)> SpawnUniques(
        FloorSpawnConfig floorConfig,
        List<Region> regions,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions)
    {
        var spawned = new List<(BaseEntity entity, int threat)>();

        if (floorConfig?.UniqueCreatures == null || floorConfig.UniqueCreatures.Count == 0)
            return spawned;

        foreach (var creatureId in floorConfig.UniqueCreatures)
        {
            // Skip if already spawned this run
            if (_spawnedUniques.Contains(creatureId))
            {
                GD.Print($"UniqueMonsterSpawner: Skipping {creatureId} - already spawned this run");
                continue;
            }

            var creatureData = _dataLoader.GetCreature(creatureId);
            if (creatureData == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: Unknown creature ID '{creatureId}'");
                continue;
            }

            // Find suitable region (prefer large regions away from entrance)
            var region = FindLairRegion(regions, metadata);
            if (region == null)
            {
                GD.PushWarning($"UniqueMonsterSpawner: No suitable region for {creatureId}");
                continue;
            }

            // Find position (prefer center of region)
            var position = FindUniquePosition(region, metadata, occupiedPositions);
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
            GD.Print($"UniqueMonsterSpawner: Spawned unique '{creatureData.Name}' at {position.Value}");
        }

        return spawned;
    }

    /// <summary>
    /// Finds a large region suitable for a unique monster lair.
    /// Prefers regions away from the entrance.
    /// </summary>
    private Region FindLairRegion(List<Region> regions, DungeonMetadata metadata)
    {
        if (regions == null || regions.Count == 0)
            return null;

        // Filter to large regions (at least 25 tiles)
        var largeRegions = regions.Where(r => r.Area >= 25).ToList();
        if (largeRegions.Count == 0)
            largeRegions = regions; // Fallback to any region

        // If we have entrance distance, prefer regions far from entrance
        if (metadata?.EntranceDistance != null)
        {
            return largeRegions
                .OrderByDescending(r => metadata.EntranceDistance.GetDistance(r.Centroid))
                .FirstOrDefault();
        }

        // Otherwise prefer the largest region
        return largeRegions.OrderByDescending(r => r.Area).FirstOrDefault();
    }

    /// <summary>
    /// Finds a position for a unique monster, preferring the center of the region.
    /// </summary>
    private GridPosition? FindUniquePosition(
        Region region,
        DungeonMetadata metadata,
        HashSet<Vector2I> occupiedPositions)
    {
        // Try centroid first
        var centroid = region.Centroid;
        if (!occupiedPositions.Contains(new Vector2I(centroid.X, centroid.Y)) &&
            region.Tiles.Contains(centroid))
        {
            return centroid;
        }

        // Try tiles near centroid
        var nearCenter = region.Tiles
            .Where(t => !occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
            .OrderBy(t => Mathf.Abs(t.X - centroid.X) + Mathf.Abs(t.Y - centroid.Y))
            .ToList();

        return nearCenter.FirstOrDefault();
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
