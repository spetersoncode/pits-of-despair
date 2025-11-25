using System;
using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Spawning.Data;
using PitsOfDespair.Systems.Spawning.Placement;

namespace PitsOfDespair.Systems.Spawning.Strategies;

/// <summary>
/// Spawns monster bands/packs with a leader and followers.
/// Inspired by DCSS band spawning mechanics.
/// </summary>
public class BandSpawnStrategy : ISpawnStrategy
{
    public string Name => "band";

    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly DataLoader _dataLoader;
    private readonly Dictionary<string, IPlacementStrategy> _placementStrategies;

    public BandSpawnStrategy(
        EntityFactory entityFactory,
        EntityManager entityManager,
        DataLoader dataLoader,
        Dictionary<string, IPlacementStrategy> placementStrategies)
    {
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _dataLoader = dataLoader;
        _placementStrategies = placementStrategies;
    }

    public SpawnResult Execute(
        SpawnEntryData entry,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions)
    {
        var result = new SpawnResult();

        // Get band data from inline definition or external reference
        BandData bandData = null;

        if (entry.Band != null)
        {
            // Use inline band definition
            bandData = entry.Band;
        }
        else if (!string.IsNullOrEmpty(entry.BandId))
        {
            // Load from external file
            bandData = _dataLoader.GetBand(entry.BandId);
            if (bandData == null)
            {
                GD.PushWarning($"BandSpawnStrategy: Band '{entry.BandId}' not found");
                return result;
            }
        }
        else
        {
            GD.PushWarning($"BandSpawnStrategy: No band definition or bandId specified");
            return result;
        }

        if (!bandData.IsValid())
        {
            GD.PushWarning($"BandSpawnStrategy: Band is invalid");
            return result;
        }

        // Spawn leader first
        var (leaderEntity, leaderPosition) = SpawnLeader(
            bandData.Leader,
            availableTiles,
            occupiedPositions
        );

        if (leaderEntity == null || !leaderPosition.HasValue)
        {
            GD.PushWarning($"BandSpawnStrategy: Failed to spawn leader for band '{entry.BandId}'");
            return result;
        }

        // Add leader-specific AI components if defined in band data
        if (bandData.Leader.Ai != null && bandData.Leader.Ai.Count > 0)
        {
            _entityFactory.AddAIComponents(leaderEntity, bandData.Leader.Ai);
        }

        result.SpawnedPositions.Add(leaderPosition.Value);
        result.EntityCount++;

        // Spawn followers around leader
        foreach (var followerGroup in bandData.Followers)
        {
            var followerResult = SpawnFollowers(
                followerGroup,
                availableTiles,
                occupiedPositions,
                leaderPosition.Value,
                leaderEntity
            );

            result.SpawnedPositions.AddRange(followerResult.SpawnedPositions);
            result.EntityCount += followerResult.EntityCount;
        }

        return result;
    }

    private (BaseEntity? entity, Vector2I? position) SpawnLeader(
        BandLeaderData leaderData,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions)
    {
        if (!leaderData.IsValid())
        {
            return (null, null);
        }

        // Get placement strategy for leader
        var placementStrategy = GetPlacementStrategy(leaderData.Placement);

        // Select position
        var positions = placementStrategy.SelectPositions(
            availableTiles,
            1,
            occupiedPositions
        );

        if (positions.Count == 0)
        {
            return (null, null);
        }

        var position = positions[0];
        var gridPosition = new GridPosition(position.X, position.Y);

        // Create and register leader entity
        var entity = _entityFactory.CreateCreature(leaderData.CreatureId, gridPosition);
        if (entity != null)
        {
            _entityManager.AddEntity(entity);
            occupiedPositions.Add(position);
            return (entity, position);
        }

        return (null, null);
    }

    private SpawnResult SpawnFollowers(
        BandFollowerData followerData,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions,
        Vector2I leaderPosition,
        BaseEntity leaderEntity)
    {
        var result = new SpawnResult();

        if (!followerData.IsValid())
        {
            return result;
        }

        // Determine follower count
        int count = followerData.Count.GetRandom();

        // Get placement strategy - if surrounding, create with distance parameters
        IPlacementStrategy placementStrategy;
        if (followerData.Placement.Equals("surrounding", StringComparison.OrdinalIgnoreCase))
        {
            placementStrategy = new SurroundingPlacement(
                followerData.Distance.GetMin(),
                followerData.Distance.GetMax()
            );
        }
        else
        {
            placementStrategy = GetPlacementStrategy(followerData.Placement);
        }

        // Select positions around leader
        var positions = placementStrategy.SelectPositions(
            availableTiles,
            count,
            occupiedPositions,
            leaderPosition
        );

        // Spawn followers at selected positions
        foreach (var position in positions)
        {
            var gridPosition = new GridPosition(position.X, position.Y);
            var entity = _entityFactory.CreateCreature(followerData.CreatureId, gridPosition);

            if (entity != null)
            {
                _entityManager.AddEntity(entity);
                result.SpawnedPositions.Add(position);
                result.EntityCount++;
                occupiedPositions.Add(position);

                // Wire up follower to follow the leader
                var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
                if (aiComponent != null)
                {
                    aiComponent.ProtectionTarget = leaderEntity;
                }
            }
        }

        return result;
    }

    private IPlacementStrategy GetPlacementStrategy(string placementName)
    {
        if (_placementStrategies.TryGetValue(placementName.ToLower(), out var strategy))
        {
            return strategy;
        }

        // Default to random if strategy not found
        return _placementStrategies["random"];
    }
}
