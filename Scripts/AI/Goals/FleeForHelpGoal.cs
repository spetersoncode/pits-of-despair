using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for fleeing from the player while periodically yelling for help.
/// Used by cowardly creatures like goblin scouts that alert others instead of fighting.
/// </summary>
public class FleeForHelpGoal : Goal
{
    private const float FleeScoreVisible = 85f;
    private const float FleeScoreBase = 75f;
    private const float ScoreDecayPerTurn = 10f;
    private const int YellInterval = 4; // Yell every 4 turns

    public override float CalculateScore(AIContext context)
    {
        var ai = context.AIComponent;

        // High priority when player is visible
        if (context.IsPlayerVisible)
        {
            return FleeScoreVisible;
        }

        // Continue fleeing with decaying priority after losing sight of player
        if (ai.FleeturnsRemaining > 0)
        {
            // Score decays as we spend more turns fleeing without seeing the player
            float score = FleeScoreBase - (ai.TurnsSincePlayerSeen * ScoreDecayPerTurn);
            return Mathf.Max(0f, score);
        }

        // Not fleeing
        return 0f;
    }

    public override ActionResult Execute(AIContext context)
    {
        var entity = context.Entity;
        var player = context.Player;
        var ai = context.AIComponent;

        // If player is visible, yell periodically
        if (context.IsPlayerVisible)
        {
            // Update last known position
            ai.LastKnownPlayerPosition = player.GridPosition;

            // Check if it's time to yell
            if (ai.TurnsSinceLastYell >= YellInterval)
            {
                // Yell for help and reset counter
                ai.TurnsSinceLastYell = 0;
                var yellAction = new YellForHelpAction();
                return entity.ExecuteAction(yellAction, context.ActionContext);
            }

            // Otherwise, flee from the player
            ai.TurnsSinceLastYell++;

            // Try to flee toward allies first, fallback to greedy flee
            var allyFleeResult = FleeTowardAllies(context);
            if (allyFleeResult != null)
            {
                return allyFleeResult;
            }
            return FleeFromPlayer(context);
        }
        else
        {
            // Player not visible - continue fleeing and decrement flee turns
            ai.FleeturnsRemaining--;

            // Check if it's time to yell (keep yelling while fleeing)
            if (ai.TurnsSinceLastYell >= YellInterval)
            {
                // Yell for help and reset counter
                ai.TurnsSinceLastYell = 0;
                var yellAction = new YellForHelpAction();
                return entity.ExecuteAction(yellAction, context.ActionContext);
            }

            // Increment yell counter
            ai.TurnsSinceLastYell++;

            // Try to flee toward allies first
            var allyFleeResult = FleeTowardAllies(context);
            if (allyFleeResult != null)
            {
                return allyFleeResult;
            }

            // Fallback: Flee away from last known position if we have one
            if (ai.LastKnownPlayerPosition != null)
            {
                return FleeFromPosition(context, ai.LastKnownPlayerPosition.Value);
            }

            // Final fallback: greedy flee
            return FleeFromPlayer(context);
        }
    }

    /// <summary>
    /// Attempts to flee toward nearby allies using Dijkstra pathfinding.
    /// Only moves toward allies if it maintains safe distance from player.
    /// </summary>
    private ActionResult? FleeTowardAllies(AIContext context)
    {
        const int MinSafeDistance = 3;
        var entity = context.Entity;
        var player = context.Player;
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;

        // Find nearby allies
        var allies = FindNearbyAllies(context);
        if (allies.Count == 0)
        {
            return null; // No allies nearby, use fallback
        }

        // Build Dijkstra map with allies as goals
        float[,] allyDistanceMap = DijkstraMapBuilder.BuildDistanceMap(
            allies,
            mapSystem,
            entityManager,  // Treat occupied cells as obstacles
            player          // Treat player position as obstacle
        );

        // Get next position toward nearest ally
        GridPosition? nextPos = DijkstraMapBuilder.GetNearestGoalDirection(
            entity.GridPosition,
            allyDistanceMap
        );

        if (nextPos == null)
        {
            return null; // No valid path to allies, use fallback
        }

        // Check distance constraints
        int currentDistToPlayer = DistanceHelper.ChebyshevDistance(entity.GridPosition, player.GridPosition);
        int newDistToPlayer = DistanceHelper.ChebyshevDistance(nextPos.Value, player.GridPosition);

        // If already at safe distance, allow moving slightly closer to reach allies
        // Otherwise, must maintain or increase distance
        bool moveIsValid;
        if (currentDistToPlayer >= MinSafeDistance)
        {
            // Allow moving up to 1 tile closer if we're already safe
            moveIsValid = newDistToPlayer >= currentDistToPlayer - 1;
        }
        else
        {
            // Too close to player - must maintain or increase distance
            moveIsValid = newDistToPlayer >= currentDistToPlayer;
        }

        if (!moveIsValid)
        {
            return null; // Move would bring us too close to player, use fallback
        }

        // Execute move toward ally
        Vector2I dir = new Vector2I(
            nextPos.Value.X - entity.GridPosition.X,
            nextPos.Value.Y - entity.GridPosition.Y
        );
        var moveAction = new MoveAction(dir);
        return entity.ExecuteAction(moveAction, context.ActionContext);
    }

    /// <summary>
    /// Moves the entity away from the player.
    /// Attempts to maximize distance by moving in the opposite direction.
    /// </summary>
    private ActionResult FleeFromPlayer(AIContext context)
    {
        var entity = context.Entity;
        var player = context.Player;
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;

        GridPosition entityPos = entity.GridPosition;
        GridPosition playerPos = player.GridPosition;

        // Calculate direction away from player (inverse of player direction)
        int dx = entityPos.X - playerPos.X;
        int dy = entityPos.Y - playerPos.Y;

        // Normalize to get general flee direction
        // Prioritize the larger component for more direct fleeing
        Vector2I primaryFleeDirection = Vector2I.Zero;

        if (dx != 0)
        {
            primaryFleeDirection.X = dx > 0 ? 1 : -1;
        }
        if (dy != 0)
        {
            primaryFleeDirection.Y = dy > 0 ? 1 : -1;
        }

        // If directly on top of player (shouldn't happen, but handle it)
        if (primaryFleeDirection == Vector2I.Zero)
        {
            primaryFleeDirection = Vector2I.Right; // Default to some direction
        }

        // Try flee directions in order of preference:
        // 1. Primary flee direction (diagonal or straight away)
        // 2. Perpendicular directions
        // 3. Any valid direction that increases distance
        List<Vector2I> fleeDirections = new List<Vector2I>();

        // Primary flee direction
        fleeDirections.Add(primaryFleeDirection);

        // Perpendicular directions
        if (primaryFleeDirection.X != 0 && primaryFleeDirection.Y == 0)
        {
            // Fleeing horizontally, add vertical options
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, 1));
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, -1));
            fleeDirections.Add(new Vector2I(0, 1));
            fleeDirections.Add(new Vector2I(0, -1));
        }
        else if (primaryFleeDirection.Y != 0 && primaryFleeDirection.X == 0)
        {
            // Fleeing vertically, add horizontal options
            fleeDirections.Add(new Vector2I(1, primaryFleeDirection.Y));
            fleeDirections.Add(new Vector2I(-1, primaryFleeDirection.Y));
            fleeDirections.Add(new Vector2I(1, 0));
            fleeDirections.Add(new Vector2I(-1, 0));
        }
        else
        {
            // Fleeing diagonally, add adjacent diagonal and straight options
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, 0));
            fleeDirections.Add(new Vector2I(0, primaryFleeDirection.Y));
        }

        // All 8 directions as fallback
        Vector2I[] allDirections = {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right,
            new Vector2I(-1, -1), new Vector2I(1, -1),
            new Vector2I(-1, 1), new Vector2I(1, 1)
        };

        foreach (Vector2I dir in allDirections)
        {
            if (!fleeDirections.Contains(dir))
            {
                fleeDirections.Add(dir);
            }
        }

        // Try each direction in priority order
        int currentDistance = DistanceHelper.ChebyshevDistance(entityPos, playerPos);

        foreach (Vector2I dir in fleeDirections)
        {
            GridPosition newPos = new GridPosition(
                entityPos.X + dir.X,
                entityPos.Y + dir.Y
            );

            // Check if walkable and not occupied
            if (mapSystem.IsWalkable(newPos) && entityManager.GetEntityAtPosition(newPos) == null)
            {
                // Prefer directions that increase distance from player
                int newDistance = DistanceHelper.ChebyshevDistance(newPos, playerPos);
                if (newDistance >= currentDistance)
                {
                    var moveAction = new MoveAction(dir);
                    return entity.ExecuteAction(moveAction, context.ActionContext);
                }
            }
        }

        // If no direction increases distance, try any valid move (even if it brings us closer)
        foreach (Vector2I dir in fleeDirections)
        {
            GridPosition newPos = new GridPosition(
                entityPos.X + dir.X,
                entityPos.Y + dir.Y
            );

            if (mapSystem.IsWalkable(newPos) && entityManager.GetEntityAtPosition(newPos) == null)
            {
                var moveAction = new MoveAction(dir);
                return entity.ExecuteAction(moveAction, context.ActionContext);
            }
        }

        // Completely cornered - can't flee
        return new ActionResult
        {
            Success = false,
            Message = "Cannot flee, cornered!",
            ConsumesTurn = true
        };
    }

    /// <summary>
    /// Moves the entity away from a specific position.
    /// Used when fleeing from last known player position.
    /// </summary>
    private ActionResult FleeFromPosition(AIContext context, GridPosition threatPosition)
    {
        var entity = context.Entity;
        var mapSystem = context.MapSystem;
        var entityManager = context.EntityManager;

        GridPosition entityPos = entity.GridPosition;

        // Calculate direction away from threat position
        int dx = entityPos.X - threatPosition.X;
        int dy = entityPos.Y - threatPosition.Y;

        // Normalize to get general flee direction
        Vector2I primaryFleeDirection = Vector2I.Zero;

        if (dx != 0)
        {
            primaryFleeDirection.X = dx > 0 ? 1 : -1;
        }
        if (dy != 0)
        {
            primaryFleeDirection.Y = dy > 0 ? 1 : -1;
        }

        // If on top of threat position, pick a random direction
        if (primaryFleeDirection == Vector2I.Zero)
        {
            primaryFleeDirection = Vector2I.Right;
        }

        // Try directions in order of preference
        List<Vector2I> fleeDirections = new List<Vector2I>();
        fleeDirections.Add(primaryFleeDirection);

        // Add perpendicular and adjacent directions
        if (primaryFleeDirection.X != 0 && primaryFleeDirection.Y == 0)
        {
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, 1));
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, -1));
        }
        else if (primaryFleeDirection.Y != 0 && primaryFleeDirection.X == 0)
        {
            fleeDirections.Add(new Vector2I(1, primaryFleeDirection.Y));
            fleeDirections.Add(new Vector2I(-1, primaryFleeDirection.Y));
        }
        else
        {
            fleeDirections.Add(new Vector2I(primaryFleeDirection.X, 0));
            fleeDirections.Add(new Vector2I(0, primaryFleeDirection.Y));
        }

        // All 8 directions as fallback
        Vector2I[] allDirections = {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right,
            new Vector2I(-1, -1), new Vector2I(1, -1),
            new Vector2I(-1, 1), new Vector2I(1, 1)
        };

        foreach (Vector2I dir in allDirections)
        {
            if (!fleeDirections.Contains(dir))
            {
                fleeDirections.Add(dir);
            }
        }

        // Try each direction in priority order
        int currentDistance = DistanceHelper.ChebyshevDistance(entityPos, threatPosition);

        foreach (Vector2I dir in fleeDirections)
        {
            GridPosition newPos = new GridPosition(
                entityPos.X + dir.X,
                entityPos.Y + dir.Y
            );

            if (mapSystem.IsWalkable(newPos) && entityManager.GetEntityAtPosition(newPos) == null)
            {
                int newDistance = DistanceHelper.ChebyshevDistance(newPos, threatPosition);
                if (newDistance >= currentDistance)
                {
                    var moveAction = new MoveAction(dir);
                    return entity.ExecuteAction(moveAction, context.ActionContext);
                }
            }
        }

        // If no direction increases distance, try any valid move
        foreach (Vector2I dir in fleeDirections)
        {
            GridPosition newPos = new GridPosition(
                entityPos.X + dir.X,
                entityPos.Y + dir.Y
            );

            if (mapSystem.IsWalkable(newPos) && entityManager.GetEntityAtPosition(newPos) == null)
            {
                var moveAction = new MoveAction(dir);
                return entity.ExecuteAction(moveAction, context.ActionContext);
            }
        }

        // Completely cornered
        return new ActionResult
        {
            Success = false,
            Message = "Cannot flee, cornered!",
            ConsumesTurn = true
        };
    }

    /// <summary>
    /// Finds nearby allies that can help (creatures with SearchLastKnown goal).
    /// </summary>
    private List<GridPosition> FindNearbyAllies(AIContext context)
    {
        const int AllyDetectionRadius = 20;
        var entity = context.Entity;
        var allies = new List<GridPosition>();

        var allEntities = context.EntityManager.GetAllEntities();
        foreach (var potentialAlly in allEntities)
        {
            // Skip self
            if (potentialAlly == entity)
            {
                continue;
            }

            // Check if within detection range
            int distance = DistanceHelper.ChebyshevDistance(
                entity.GridPosition,
                potentialAlly.GridPosition
            );

            if (distance > AllyDetectionRadius)
            {
                continue;
            }

            // Check if has AI and SearchLastKnown goal
            var aiComponent = potentialAlly.GetNodeOrNull<AIComponent>("AIComponent");
            if (aiComponent != null && HasSearchGoal(aiComponent))
            {
                allies.Add(potentialAlly.GridPosition);
            }
        }

        return allies;
    }

    /// <summary>
    /// Checks if an AI component has the SearchLastKnown goal.
    /// </summary>
    private bool HasSearchGoal(AIComponent aiComponent)
    {
        if (aiComponent.AvailableGoals == null)
        {
            return false;
        }

        foreach (var goal in aiComponent.AvailableGoals)
        {
            if (goal is SearchLastKnownPositionGoal)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnDeactivated(AIContext context)
    {
        // Reset yell counter when goal deactivated
        context.AIComponent.TurnsSinceLastYell = 0;
    }

    public override string GetName() => "Flee for Help";
}
