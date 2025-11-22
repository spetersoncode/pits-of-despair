using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for defending a protection target by attacking nearby enemies.
/// Used by friendly creatures to protect the player or other allies.
/// Higher priority than FollowTargetGoal when enemies are present.
/// </summary>
public class DefendTargetGoal : Goal
{
    private const float BaseScore = 80f;
    private const int DefendRadius = 5;

    public override float CalculateScore(AIContext context)
    {
        var target = context.ProtectionTarget;

        // No target to defend
        if (target == null)
            return 0f;

        // Check if there are enemies near the protection target
        var enemies = context.GetEnemiesNearProtectionTarget(DefendRadius);
        if (enemies.Count == 0)
            return 0f;

        // High priority when enemies threaten our target
        return BaseScore;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;
        var player = context.ActionContext.Player;

        // Find enemies near our protection target
        var enemies = context.GetEnemiesNearProtectionTarget(DefendRadius);
        if (enemies.Count == 0)
        {
            return new ActionResult
            {
                Success = false,
                Message = "No enemies to defend against",
                ConsumesTurn = false
            };
        }

        // Target the closest enemy
        var targetEnemy = context.GetClosestEnemy(enemies);
        if (targetEnemy == null)
        {
            return new ActionResult
            {
                Success = false,
                Message = "No valid target",
                ConsumesTurn = false
            };
        }

        int distanceToEnemy = DistanceHelper.ChebyshevDistance(entity.GridPosition, targetEnemy.GridPosition);

        // If adjacent, attack
        if (distanceToEnemy <= 1)
        {
            ai.ClearPath();
            var attackAction = new AttackAction(targetEnemy, 0);
            return entity.ExecuteAction(attackAction, context.ActionContext);
        }

        // Not adjacent - move toward enemy
        return MoveTowardTarget(context, targetEnemy.GridPosition);
    }

    /// <summary>
    /// Moves entity toward a target position using pathfinding.
    /// </summary>
    private ActionResult MoveTowardTarget(AIContext context, GridPosition target)
    {
        var entity = context.Entity;
        var ai = context.AIComponent;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;
        var player = context.ActionContext.Player;

        // Calculate path to target
        if (ai.CurrentPath.Count == 0)
        {
            var path = AStarPathfinder.FindPath(entity.GridPosition, target, mapSystem, entityManager, player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
            else
            {
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

    public override string GetName() => "Defend Target";
}
