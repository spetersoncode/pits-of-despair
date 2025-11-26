using Godot;
using PitsOfDespair.AI.Components;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for pack followers. Stays within FollowDistance of the pack leader.
/// If leader dies, the goal completes (leader promotion is handled by PackLeaderComponent).
/// </summary>
public class FollowPackLeaderGoal : Goal
{
    private PackFollowerComponent _followerComp;

    public override bool IsFinished(AIContext context)
    {
        _followerComp ??= context.Entity.GetNodeOrNull<PackFollowerComponent>("PackFollowerComponent");

        // Finished if no follower component (we might have been promoted to leader)
        if (_followerComp == null || !GodotObject.IsInstanceValid(_followerComp))
            return true;

        // Finished if leader is gone (we need to re-evaluate - might be promoted)
        if (!_followerComp.HasValidLeader)
            return true;

        return false;
    }

    public override void TakeAction(AIContext context)
    {
        // Abort patrol if we see enemies - combat takes priority
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            Fail(context);
            return;
        }

        _followerComp ??= context.Entity.GetNodeOrNull<PackFollowerComponent>("PackFollowerComponent");

        if (_followerComp == null || !_followerComp.HasValidLeader)
        {
            Fail(context);
            return;
        }

        var leader = _followerComp.Leader;
        int followDistance = _followerComp.FollowDistance;

        // Check distance to leader
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            leader.GridPosition);

        // Already close enough - stay put
        if (distance <= followDistance)
            return;

        // Move toward leader
        var approach = new ApproachGoal(
            leader,
            desiredDistance: followDistance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        var leader = _followerComp?.Leader;
        if (leader != null)
        {
            return $"Following {leader.DisplayName}";
        }
        return "Following leader";
    }
}
