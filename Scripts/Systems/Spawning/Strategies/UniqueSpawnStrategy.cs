using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Systems.Spawning.Data;
using PitsOfDespair.Systems.Spawning.Placement;

namespace PitsOfDespair.Systems.Spawning.Strategies;

/// <summary>
/// Spawns unique creatures (bosses, named enemies).
/// Future-proofing for unique creature mechanics like:
/// - Preventing duplicate spawns across floors
/// - Special positioning requirements
/// - Custom spawn conditions
/// </summary>
public class UniqueSpawnStrategy : ISpawnStrategy
{
    public string Name => "unique";

    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly Dictionary<string, IPlacementStrategy> _placementStrategies;

    // Track spawned uniques to prevent duplicates (future enhancement)
    private readonly HashSet<string> _spawnedUniques = new();

    public UniqueSpawnStrategy(
        EntityFactory entityFactory,
        EntityManager entityManager,
        Dictionary<string, IPlacementStrategy> placementStrategies)
    {
        _entityFactory = entityFactory;
        _entityManager = entityManager;
        _placementStrategies = placementStrategies;
    }

    public SpawnResult Execute(
        SpawnEntryData entry,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions)
    {
        var result = new SpawnResult();

        if (string.IsNullOrEmpty(entry.CreatureId))
        {
            GD.PushWarning($"UniqueSpawnStrategy: No creatureId specified");
            return result;
        }

        // Future: Check if unique already spawned
        // if (_spawnedUniques.Contains(entry.CreatureId))
        // {
        //     GD.Print($"UniqueSpawnStrategy: Unique '{entry.CreatureId}' already spawned, skipping");
        //     return result;
        // }

        // Get placement strategy (uniques typically use specific placement)
        var placementStrategy = GetPlacementStrategy(entry.Placement);

        // Select single position
        var positions = placementStrategy.SelectPositions(
            availableTiles,
            1,
            occupiedPositions
        );

        if (positions.Count == 0)
        {
            GD.PushWarning($"UniqueSpawnStrategy: No valid position found for unique '{entry.CreatureId}'");
            return result;
        }

        var position = positions[0];
        var gridPosition = new GridPosition(position.X, position.Y);

        // Spawn unique creature
        var entity = _entityFactory.CreateEntity(entry.CreatureId, gridPosition);
        if (entity != null)
        {
            _entityManager.AddEntity(entity);
            result.SpawnedPositions.Add(position);
            result.EntityCount++;
            occupiedPositions.Add(position);

            // Track this unique as spawned
            _spawnedUniques.Add(entry.CreatureId);

            GD.Print($"UniqueSpawnStrategy: Spawned unique '{entry.CreatureId}' at {position}");
        }

        return result;
    }

    private IPlacementStrategy GetPlacementStrategy(string placementName)
    {
        if (_placementStrategies.TryGetValue(placementName.ToLower(), out var strategy))
        {
            return strategy;
        }

        // Default to center for uniques (bosses typically in center of room)
        return _placementStrategies.ContainsKey("center")
            ? _placementStrategies["center"]
            : _placementStrategies["random"];
    }

    /// <summary>
    /// Clears the list of spawned uniques (call when descending floors or resetting).
    /// </summary>
    public void ClearSpawnedUniques()
    {
        _spawnedUniques.Clear();
    }
}
