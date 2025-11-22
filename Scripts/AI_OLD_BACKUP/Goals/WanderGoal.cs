using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for wandering randomly around the dungeon.
/// Low-priority fallback behavior for roaming creatures (like rats)
/// that don't return to a spawn point.
/// </summary>
public class WanderGoal : Goal
{
    private const float WanderScore = 20f;

    public override float CalculateScore(AIContext context)
    {
        // Always valid, but low priority
        // Will be selected when no higher-priority goals are available
        return WanderScore;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Try all 8 directions and collect valid ones
        List<Vector2I> possibleDirections = new List<Vector2I>();

        Vector2I[] allDirections = {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right,
            new Vector2I(-1, -1), new Vector2I(1, -1),
            new Vector2I(-1, 1), new Vector2I(1, 1)
        };

        foreach (Vector2I dir in allDirections)
        {
            GridPosition newPos = new GridPosition(
                entity.GridPosition.X + dir.X,
                entity.GridPosition.Y + dir.Y
            );

            // Check if walkable and not occupied
            if (mapSystem.IsWalkable(newPos) && entityManager.GetEntityAtPosition(newPos) == null)
            {
                possibleDirections.Add(dir);
            }
        }

        // Pick random valid direction
        if (possibleDirections.Count > 0)
        {
            int randomIndex = GD.RandRange(0, possibleDirections.Count - 1);
            Vector2I direction = possibleDirections[randomIndex];
            var moveAction = new MoveAction(direction);
            return entity.ExecuteAction(moveAction, context.ActionContext);
        }

        // No valid directions - just wait
        return new ActionResult
        {
            Success = true,
            Message = "No valid wander directions, waiting",
            ConsumesTurn = true
        };
    }

    public override string GetName() => "Wander";
}
