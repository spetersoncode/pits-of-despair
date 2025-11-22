using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;
using System.Collections.Generic;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that moves the entity one step in a random valid direction.
/// Completes after pushing a single MoveDirectionGoal.
/// Optionally constrained to a radius around a center point.
/// </summary>
public class WanderGoal : Goal
{
    public GridPosition? Center { get; private set; }
    public int? Radius { get; private set; }
    private bool _moved = false;

    /// <summary>
    /// Creates a wander goal with no constraints.
    /// </summary>
    public WanderGoal(Goal originalIntent = null)
    {
        OriginalIntent = originalIntent;
    }

    /// <summary>
    /// Creates a wander goal constrained to a radius around a center point.
    /// Used for searching behavior.
    /// </summary>
    public WanderGoal(GridPosition center, int radius, Goal originalIntent = null)
    {
        Center = center;
        Radius = radius;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        return _moved;
    }

    public override void TakeAction(AIContext context)
    {
        var directions = GetValidDirections(context);
        if (directions.Count == 0)
        {
            // No valid directions - just mark as done (wait in place)
            _moved = true;
            return;
        }

        // Pick random valid direction
        var dir = directions[GD.RandRange(0, directions.Count - 1)];
        var moveGoal = new MoveDirectionGoal(dir, originalIntent: this);
        context.AIComponent.GoalStack.Push(moveGoal);
        _moved = true;
    }

    private List<Vector2I> GetValidDirections(AIContext context)
    {
        var validDirs = new List<Vector2I>();
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;
        var currentPos = context.Entity.GridPosition;

        // All 8 directions
        var directions = new Vector2I[]
        {
            new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, -1),
            new Vector2I(-1, 0),                       new Vector2I(1, 0),
            new Vector2I(-1, 1),  new Vector2I(0, 1),  new Vector2I(1, 1)
        };

        foreach (var dir in directions)
        {
            var targetPos = new GridPosition(currentPos.X + dir.X, currentPos.Y + dir.Y);

            // Check if walkable
            if (!mapSystem.IsWalkable(targetPos))
                continue;

            // Check if occupied by another entity
            var entityAtPos = entityManager.GetEntityAtPosition(targetPos);
            if (entityAtPos != null)
                continue;

            // Check radius constraint if present
            if (Center.HasValue && Radius.HasValue)
            {
                int distFromCenter = DistanceHelper.ChebyshevDistance(targetPos, Center.Value);
                if (distFromCenter > Radius.Value)
                    continue;
            }

            validDirs.Add(dir);
        }

        return validDirs;
    }

    public override string GetName()
    {
        if (Center.HasValue)
            return $"Wander (around {Center.Value}, radius {Radius})";
        return "Wander";
    }
}
