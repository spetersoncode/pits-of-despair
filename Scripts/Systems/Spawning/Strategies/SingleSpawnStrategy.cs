using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Systems.Spawning.Data;
using PitsOfDespair.Systems.Spawning.Placement;

namespace PitsOfDespair.Systems.Spawning.Strategies;

/// <summary>
/// Spawns individual creatures or items at random or specified positions.
/// </summary>
public class SingleSpawnStrategy : ISpawnStrategy
{
    public string Name => "single";

    private readonly EntityFactory _entityFactory;
    private readonly EntityManager _entityManager;
    private readonly Dictionary<string, IPlacementStrategy> _placementStrategies;

    public SingleSpawnStrategy(
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

        if (!entry.IsValid())
        {
            GD.PushWarning($"SingleSpawnStrategy: Invalid spawn entry");
            return result;
        }

        // Determine spawn count
        int count = entry.Count.GetRandom();

        // Get placement strategy (always random for single/multiple spawns)
        var placementStrategy = GetPlacementStrategy("random");

        // Select positions
        var positions = placementStrategy.SelectPositions(
            availableTiles,
            count,
            occupiedPositions
        );

        // Spawn entities at selected positions
        foreach (var position in positions)
        {
            var gridPosition = new GridPosition(position.X, position.Y);

            // Spawn creature or item based on entry type
            var entity = !string.IsNullOrEmpty(entry.CreatureId)
                ? _entityFactory.CreateCreature(entry.CreatureId, gridPosition)
                : _entityFactory.CreateItem(entry.ItemId, gridPosition);

            if (entity != null)
            {
                _entityManager.AddEntity(entity);
                result.SpawnedPositions.Add(position);
                result.EntityCount++;

                // Mark position as occupied
                occupiedPositions.Add(position);
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
