using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that follows the protection target, staying within FollowDistance.
/// Used by friendly creatures to stay near their VIP.
/// </summary>
public class FollowTargetGoal : Goal
{
    public FollowTargetGoal(Goal originalIntent = null)
    {
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        var target = context.ProtectionTarget;
        if (target == null || !Godot.GodotObject.IsInstanceValid(target))
            return true; // No target to follow

        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            target.GridPosition);

        return distance <= context.AIComponent.FollowDistance;
    }

    public override void TakeAction(AIContext context)
    {
        var target = context.ProtectionTarget;
        if (target == null || !Godot.GodotObject.IsInstanceValid(target))
        {
            Fail(context);
            return;
        }

        // Push ApproachGoal to move toward protection target
        var approach = new ApproachGoal(target, desiredDistance: context.AIComponent.FollowDistance, originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName() => "Follow target";
}
