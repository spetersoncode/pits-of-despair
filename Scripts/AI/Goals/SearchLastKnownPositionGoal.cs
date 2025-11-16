using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for investigating the last known position where the player was seen.
/// Paths to the position, then wanders nearby searching for the target.
/// </summary>
public class SearchLastKnownPositionGoal : Goal
{
    private const float BaseSearchScore = 70f;
    private const float ScoreDecayPerTurn = 10f;

    public override float CalculateScore(AIContext context)
    {
        var ai = context.AIComponent;

        // Can't search if we don't have a last known position
        if (ai.LastKnownPlayerPosition == null)
        {
            return 0f;
        }

        // Can't search if we've exhausted our search turns
        if (ai.SearchTurnsRemaining <= 0)
        {
            return 0f;
        }

        // Score decays as we spend more time searching
        // Starts high when player just lost, decreases over turns
        float score = BaseSearchScore - (ai.TurnsSincePlayerSeen * ScoreDecayPerTurn);

        // Never go below 0
        return Mathf.Max(0f, score);
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.MapSystem;

        if (ai.LastKnownPlayerPosition == null)
        {
            return new ActionResult
            {
                Success = false,
                Message = "No last known position to search",
                ConsumesTurn = false
            };
        }

        GridPosition lastKnown = ai.LastKnownPlayerPosition.Value;

        // If not at last known position, path there
        if (!entity.GridPosition.Equals(lastKnown))
        {
            return MoveTowardTarget(context, lastKnown);
        }

        // At last known position - wander randomly within search radius
        var result = WanderNearPosition(context, lastKnown);

        // Decrement investigation turns
        ai.SearchTurnsRemaining--;

        // If search is exhausted, clear last known position
        if (ai.SearchTurnsRemaining <= 0)
        {
            ai.LastKnownPlayerPosition = null;
        }

        return result;
    }

    /// <summary>
    /// Moves entity toward a target position using pathfinding.
    /// </summary>
    private ActionResult MoveTowardTarget(AIContext context, GridPosition target)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;
        var player = context.Player;

        // If we don't have a path, calculate one
        if (ai.CurrentPath.Count == 0)
        {
            var path = AStarPathfinder.FindPath(entity.GridPosition, target, mapSystem, entityManager, player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
            else
            {
                // No path - just wander instead
                return WanderNearPosition(context, target);
            }
        }

        // Get next position in path
        GridPosition? nextPos = ai.GetNextPosition();
        if (nextPos == null)
        {
            return WanderNearPosition(context, target);
        }

        // Validate next position isn't occupied
        if (entityManager.GetEntityAtPosition(nextPos.Value) != null)
        {
            // Blocked - repath
            ai.ClearPath();
            var newPath = AStarPathfinder.FindPath(entity.GridPosition, target, mapSystem, entityManager, player);
            if (newPath != null && newPath.Count > 0)
            {
                ai.CurrentPath = newPath;
                GridPosition? newNextPos = ai.GetNextPosition();
                if (newNextPos != null && entityManager.GetEntityAtPosition(newNextPos.Value) == null)
                {
                    Vector2I direction = new Vector2I(
                        newNextPos.Value.X - entity.GridPosition.X,
                        newNextPos.Value.Y - entity.GridPosition.Y
                    );
                    var moveAction = new MoveAction(direction);
                    return entity.ExecuteAction(moveAction, context.ActionContext);
                }
            }
            // Couldn't repath - wander instead
            return WanderNearPosition(context, target);
        }

        // Move to next position
        Vector2I dir = new Vector2I(
            nextPos.Value.X - entity.GridPosition.X,
            nextPos.Value.Y - entity.GridPosition.Y
        );
        var action = new MoveAction(dir);
        return entity.ExecuteAction(action, context.ActionContext);
    }

    /// <summary>
    /// Wanders randomly within search radius of a center position.
    /// </summary>
    private ActionResult WanderNearPosition(AIContext context, GridPosition center)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.MapSystem;

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

            // Check if within search radius and walkable
            int distance = DistanceHelper.ChebyshevDistance(newPos, center);
            if (distance <= ai.SearchRadius && mapSystem.IsWalkable(newPos))
            {
                possibleDirections.Add(dir);
            }
        }

        // Pick random direction
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
            Message = "No valid wander directions",
            ConsumesTurn = true
        };
    }

    public override string GetName() => "Search Last Known";
}
