using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;
using System.Collections.Generic;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that picks a random distant walkable position and paths toward it.
/// Composes with ApproachGoal to handle the actual pathfinding.
/// Completes when the entity reaches the destination.
/// </summary>
public class PatrolGoal : Goal
{
    public GridPosition Destination { get; private set; }
    public int MinDistance { get; private set; }
    private bool _destinationChosen = false;

    /// <summary>
    /// Creates a patrol goal that will pick a random distant destination.
    /// </summary>
    /// <param name="minDistance">Minimum distance from current position for patrol destination.</param>
    /// <param name="originalIntent">The goal that spawned this patrol.</param>
    public PatrolGoal(int minDistance = 10, Goal originalIntent = null)
    {
        MinDistance = minDistance;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        if (!_destinationChosen)
            return false;

        // Finished when we reach the destination
        int distance = DistanceHelper.ChebyshevDistance(context.Entity.GridPosition, Destination);
        return distance <= 1;
    }

    public override void TakeAction(AIContext context)
    {
        // Pick destination once
        if (!_destinationChosen)
        {
            var destination = FindPatrolDestination(context);
            if (destination == null)
            {
                Fail(context);
                return;
            }
            Destination = destination.Value;
            _destinationChosen = true;
        }

        // Not there yet - push approach goal to continue pathfinding
        var approach = new ApproachGoal(Destination, desiredDistance: 1, originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    private GridPosition? FindPatrolDestination(AIContext context)
    {
        var mapSystem = context.ActionContext.MapSystem;
        var currentPos = context.Entity.GridPosition;

        // Sample random positions to find a distant walkable tile
        var candidates = new List<GridPosition>();
        int mapWidth = mapSystem.MapWidth;
        int mapHeight = mapSystem.MapHeight;

        const int maxAttempts = 50;
        for (int i = 0; i < maxAttempts; i++)
        {
            int x = GD.RandRange(1, mapWidth - 2);
            int y = GD.RandRange(1, mapHeight - 2);
            var pos = new GridPosition(x, y);

            if (!mapSystem.IsWalkable(pos))
                continue;

            int distance = DistanceHelper.ChebyshevDistance(currentPos, pos);
            if (distance >= MinDistance)
            {
                candidates.Add(pos);
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[GD.RandRange(0, candidates.Count - 1)];
    }

    public override string GetName() => _destinationChosen
        ? $"Patrol to {Destination}"
        : "Patrol (choosing destination)";
}
