using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that pathfinds to a target position or entity and pushes MoveDirectionGoals.
/// When tracking an entity, automatically updates target position each turn.
/// </summary>
public class ApproachGoal : Goal
{
    public GridPosition TargetPosition { get; private set; }
    public int DesiredDistance { get; private set; }

    /// <summary>
    /// Optional entity to track. If set, TargetPosition is updated each turn.
    /// </summary>
    public BaseEntity TargetEntity { get; private set; }

    private int _pathfindCooldown = 0;
    private const int PathfindEveryNTurns = 4;

    /// <summary>
    /// Creates an approach goal to get within a desired distance of a static position.
    /// </summary>
    public ApproachGoal(GridPosition target, int desiredDistance = 1, Goal originalIntent = null)
    {
        TargetPosition = target;
        DesiredDistance = desiredDistance;
        OriginalIntent = originalIntent;
    }

    /// <summary>
    /// Creates an approach goal to get within a desired distance of a moving entity.
    /// The target position is updated each turn to track the entity.
    /// </summary>
    public ApproachGoal(BaseEntity targetEntity, int desiredDistance = 1, Goal originalIntent = null)
    {
        TargetEntity = targetEntity;
        TargetPosition = targetEntity.GridPosition;
        DesiredDistance = desiredDistance;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        // Update target position if tracking an entity
        if (TargetEntity != null && GodotObject.IsInstanceValid(TargetEntity))
        {
            TargetPosition = TargetEntity.GridPosition;
        }

        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            TargetPosition);
        return distance <= DesiredDistance;
    }

    public override void TakeAction(AIContext context)
    {
        // Update target position if tracking an entity
        if (TargetEntity != null && GodotObject.IsInstanceValid(TargetEntity))
        {
            TargetPosition = TargetEntity.GridPosition;
        }

        _pathfindCooldown++;

        // Recalculate path periodically or if we're the top goal (no moves queued)
        if (_pathfindCooldown >= PathfindEveryNTurns ||
            context.AIComponent.GoalStack.Peek() == this)
        {
            CalculateAndPushMoves(context);
            _pathfindCooldown = 0;
        }
    }

    private void CalculateAndPushMoves(AIContext context)
    {
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;
        var player = context.ActionContext.Player;

        // Build navigation weight map for this creature
        var weights = NavigationWeightMap.Build(context.Entity, mapSystem, entityManager);

        // Find path using weighted A* pathfinder
        var path = AStarPathfinder.FindPath(
            context.Entity.GridPosition,
            TargetPosition,
            mapSystem,
            entityManager,
            player,
            weights);

        if (path == null || path.Count == 0)
        {
            Fail(context);
            return;
        }

        // Get the first step in the path
        var nextPos = path.Peek();
        var direction = new Vector2I(
            nextPos.X - context.Entity.GridPosition.X,
            nextPos.Y - context.Entity.GridPosition.Y);

        // Push a single move goal - ApproachGoal will push another next turn if needed
        var moveGoal = new MoveDirectionGoal(direction, originalIntent: this);
        context.AIComponent.GoalStack.Push(moveGoal);
    }

    public override string GetName() => TargetEntity != null
        ? $"Approach {TargetEntity.DisplayName}"
        : $"Approach {TargetPosition}";
}
