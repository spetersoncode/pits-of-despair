using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that follows a target entity, staying within a specified distance.
/// Used by both bodyguards (following protection target) and pack followers (following leader).
/// Aborts when enemies become visible to allow combat.
/// </summary>
public class FollowEntityGoal : Goal
{
    private readonly BaseEntity _target;
    private readonly int _followDistance;

    public FollowEntityGoal(BaseEntity target, int followDistance, Goal originalIntent = null)
    {
        _target = target;
        _followDistance = followDistance;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        // Abort if we see enemies - combat takes priority
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return true;

        // Finished if target is gone
        if (_target == null || !GodotObject.IsInstanceValid(_target))
            return true;

        // Finished if close enough
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            _target.GridPosition);

        return distance <= _followDistance;
    }

    public override void TakeAction(AIContext context)
    {
        // Abort if we see enemies - combat takes priority
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            Fail(context);
            return;
        }

        // Fail if target is gone
        if (_target == null || !GodotObject.IsInstanceValid(_target))
        {
            Fail(context);
            return;
        }

        // Check distance - might already be close enough
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            _target.GridPosition);

        if (distance <= _followDistance)
            return; // Close enough, wait

        // Move toward target
        var approach = new ApproachGoal(
            _target,
            desiredDistance: _followDistance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target))
            return $"Following {_target.DisplayName}";
        return "Following target";
    }
}
