using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for returning to the spawn position.
/// Score increases the longer the creature goes without seeing the player
/// and has no last known location to investigate (for guard-type creatures).
/// </summary>
public class ReturnToSpawnGoal : Goal
{
    private const float BaseScore = 10f;
    private const float ScoreIncreasePerTurn = 3f;

    public override float CalculateScore(AIContext context)
    {
        var ai = context.AIComponent;
        var entity = context.Entity;

        // Don't return if player is visible (MeleeAttack should handle that)
        if (context.IsPlayerVisible)
        {
            return 0f;
        }

        // Don't return if we have a valid search target (SearchLastKnown should handle that)
        if (ai.LastKnownPlayerPosition != null && ai.SearchTurnsRemaining > 0)
        {
            return 0f;
        }

        // Already at spawn - very low score (let Idle take over)
        if (entity.GridPosition.Equals(ai.SpawnPosition))
        {
            return 5f;
        }

        // Score increases the longer without player contact
        // This makes the creature eventually give up and return home
        float score = BaseScore + (ai.TurnsSincePlayerSeen * ScoreIncreasePerTurn);

        return score;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;
        var player = context.Player;

        // Already at spawn - just wait
        if (entity.GridPosition.Equals(ai.SpawnPosition))
        {
            ai.ClearPath();
            return new ActionResult
            {
                Success = true,
                Message = "At spawn position",
                ConsumesTurn = true
            };
        }

        // Calculate path to spawn if needed
        if (ai.CurrentPath.Count == 0)
        {
            var path = AStarPathfinder.FindPath(entity.GridPosition, ai.SpawnPosition, mapSystem, entityManager, player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
            else
            {
                // Can't path to spawn - just wait
                return new ActionResult
                {
                    Success = false,
                    Message = "No path to spawn",
                    ConsumesTurn = true
                };
            }
        }

        // Get next position in path
        GridPosition? nextPos = ai.GetNextPosition();
        if (nextPos == null)
        {
            return new ActionResult
            {
                Success = false,
                Message = "No next position in path",
                ConsumesTurn = true
            };
        }

        // Validate next position isn't occupied
        if (entityManager.GetEntityAtPosition(nextPos.Value) != null)
        {
            // Blocked - repath
            ai.ClearPath();
            var newPath = AStarPathfinder.FindPath(entity.GridPosition, ai.SpawnPosition, mapSystem, entityManager, player);
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

            // Couldn't repath - wait
            return new ActionResult
            {
                Success = false,
                Message = "Path blocked, couldn't repath",
                ConsumesTurn = true
            };
        }

        // Move to next position
        Vector2I dir = new Vector2I(
            nextPos.Value.X - entity.GridPosition.X,
            nextPos.Value.Y - entity.GridPosition.Y
        );
        var action = new MoveAction(dir);
        return entity.ExecuteAction(action, context.ActionContext);
    }

    public override string GetName() => "Return to Spawn";
}
