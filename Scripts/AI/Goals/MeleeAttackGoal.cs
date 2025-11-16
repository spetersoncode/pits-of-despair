using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for engaging visible enemies with melee attacks.
/// Handles both movement toward the target (chasing) and attacking when in range.
/// </summary>
public class MeleeAttackGoal : Goal
{
    private const float VisibleEnemyScore = 90f;

    public override float CalculateScore(AIContext context)
    {
        // Only valid when player is visible
        if (!context.IsPlayerVisible)
        {
            return 0f;
        }

        // High priority when enemy is visible
        return VisibleEnemyScore;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var player = context.Player;
        var ai = context.AIComponent;

        int distanceToPlayer = DistanceHelper.ChebyshevDistance(entity.GridPosition, player.GridPosition);

        // Update last known position since we can see the player
        ai.LastKnownPlayerPosition = player.GridPosition;
        ai.TurnsSincePlayerSeen = 0;

        // If adjacent to player, attack
        if (distanceToPlayer <= 1)
        {
            // Clear path since we're attacking in place
            ai.ClearPath();

            // Use first attack from AttackComponent (index 0)
            var attackAction = new AttackAction(player, 0);
            return entity.ExecuteAction(attackAction, context.ActionContext);
        }

        // Not adjacent - move toward player
        return MoveTowardTarget(context, player.GridPosition);
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

        // If we don't have a path or reached the end, calculate new path
        if (ai.CurrentPath.Count == 0)
        {
            var path = AStarPathfinder.FindPath(entity.GridPosition, target, mapSystem, entityManager, player);
            if (path != null)
            {
                ai.CurrentPath = path;
            }
            else
            {
                // No path available - can't move
                return new ActionResult
                {
                    Success = false,
                    Message = "No path to target",
                    ConsumesTurn = false
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
                ConsumesTurn = false
            };
        }

        // Validate next position isn't occupied by another creature
        if (IsPositionOccupiedByCreature(nextPos.Value, entityManager))
        {
            // Position is blocked - clear path and repath around the obstacle
            ai.ClearPath();

            // Recalculate path around the blocking creature
            var newPath = AStarPathfinder.FindPath(entity.GridPosition, target, mapSystem, entityManager, player);
            if (newPath != null && newPath.Count > 0)
            {
                ai.CurrentPath = newPath;

                // Try to move on the new path
                GridPosition? newNextPos = ai.GetNextPosition();
                if (newNextPos != null && !IsPositionOccupiedByCreature(newNextPos.Value, entityManager))
                {
                    // New path is clear, move along it
                    Vector2I newDirection = new Vector2I(
                        newNextPos.Value.X - entity.GridPosition.X,
                        newNextPos.Value.Y - entity.GridPosition.Y
                    );

                    var repathMoveAction = new MoveAction(newDirection);
                    return entity.ExecuteAction(repathMoveAction, context.ActionContext);
                }
            }

            // Couldn't repath - wait this turn
            return new ActionResult
            {
                Success = false,
                Message = "Path blocked, couldn't repath",
                ConsumesTurn = true
            };
        }

        // Calculate direction to next position
        Vector2I direction = new Vector2I(
            nextPos.Value.X - entity.GridPosition.X,
            nextPos.Value.Y - entity.GridPosition.Y
        );

        // Execute movement
        var moveAction = new MoveAction(direction);
        return entity.ExecuteAction(moveAction, context.ActionContext);
    }

    /// <summary>
    /// Checks if a position is occupied by another creature (not the player).
    /// </summary>
    private bool IsPositionOccupiedByCreature(GridPosition position, EntityManager entityManager)
    {
        return entityManager.GetEntityAtPosition(position) != null;
    }

    public override string GetName() => "Melee Attack";
}
