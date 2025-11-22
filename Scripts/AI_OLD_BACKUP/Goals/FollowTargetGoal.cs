using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for following a protection target (typically the player for friendly creatures).
/// Activates when the entity is too far from its protection target.
/// </summary>
public class FollowTargetGoal : Goal
{
    private const float BaseScore = 50f;

    public override float CalculateScore(AIContext context)
    {
        var target = context.ProtectionTarget;

        // No target to follow
        if (target == null)
            return 0f;

        var entity = context.Entity;
        var ai = context.AIComponent;
        int followDistance = ai.FollowDistance;

        int distanceToTarget = DistanceHelper.ChebyshevDistance(entity.GridPosition, target.GridPosition);

        // Already close enough - low priority
        if (distanceToTarget <= followDistance)
            return 0f;

        // Priority increases the further away we are
        // But should be lower than DefendTargetGoal when enemies are present
        return BaseScore + (distanceToTarget - followDistance) * 5f;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var target = context.ProtectionTarget;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;
        var player = context.ActionContext.Player;

        if (target == null)
        {
            return new ActionResult
            {
                Success = false,
                Message = "No protection target",
                ConsumesTurn = false
            };
        }

        int distanceToTarget = DistanceHelper.ChebyshevDistance(entity.GridPosition, target.GridPosition);

        // Already close enough - just wait
        if (distanceToTarget <= ai.FollowDistance)
        {
            ai.ClearPath();
            return new ActionResult
            {
                Success = true,
                Message = "Close to target",
                ConsumesTurn = true
            };
        }

        // Calculate path to target if needed
        if (ai.CurrentPath.Count == 0)
        {
            var path = AStarPathfinder.FindPath(entity.GridPosition, target.GridPosition, mapSystem, entityManager, player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
            else
            {
                // Can't path to target - just wait
                return new ActionResult
                {
                    Success = false,
                    Message = "No path to target",
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

        // Validate next position isn't occupied by another creature
        var entityAtNext = entityManager.GetEntityAtPosition(nextPos.Value);
        if (entityAtNext != null && entityAtNext != target)
        {
            // Blocked - repath
            ai.ClearPath();
            var newPath = AStarPathfinder.FindPath(entity.GridPosition, target.GridPosition, mapSystem, entityManager, player);
            if (newPath != null && newPath.Count > 0)
            {
                ai.CurrentPath = newPath;
                GridPosition? newNextPos = ai.GetNextPosition();
                if (newNextPos != null)
                {
                    var newEntityAtNext = entityManager.GetEntityAtPosition(newNextPos.Value);
                    if (newEntityAtNext == null || newEntityAtNext == target)
                    {
                        Vector2I direction = new Vector2I(
                            newNextPos.Value.X - entity.GridPosition.X,
                            newNextPos.Value.Y - entity.GridPosition.Y
                        );
                        var moveAction = new MoveAction(direction);
                        return entity.ExecuteAction(moveAction, context.ActionContext);
                    }
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

    public override string GetName() => "Follow Target";
}
