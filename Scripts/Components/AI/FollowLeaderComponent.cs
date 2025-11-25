using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// Makes creature follow their protection target (leader) when bored.
/// Responds to OnIAmBored to push FollowTargetGoal with configurable distance.
/// Used by pack followers to stay near their leader.
/// </summary>
public partial class FollowLeaderComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Desired distance to maintain from the leader.
    /// Will move closer if further than this distance.
    /// </summary>
    [Export] public int FollowDistance { get; set; } = 3;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        var target = args.Context.ProtectionTarget;
        if (target == null || !IsInstanceValid(target))
            return;

        // Combat takes precedence - don't follow if enemies visible
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Check if already close enough
        int distance = DistanceHelper.ChebyshevDistance(
            args.Context.Entity.GridPosition,
            target.GridPosition);

        if (distance <= FollowDistance)
            return;

        // Update AIComponent's follow distance to match our setting
        args.Context.AIComponent.FollowDistance = FollowDistance;

        // Push follow goal to move toward leader
        var followGoal = new FollowTargetGoal();
        args.Context.AIComponent.GoalStack.Push(followGoal);
        args.Handled = true;
    }
}
