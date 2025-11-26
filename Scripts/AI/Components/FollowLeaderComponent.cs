using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Makes creature follow their protection target when bored.
/// Responds to OnIAmBored to push FollowEntityGoal with configurable distance.
/// Used by friendly creatures (bodyguards) to stay near their VIP.
/// </summary>
public partial class FollowLeaderComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Desired distance to maintain from the protection target.
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

        // Push follow goal to move toward protection target
        var followGoal = new FollowEntityGoal(target, FollowDistance);
        args.Context.AIComponent.GoalStack.Push(followGoal);
        args.Handled = true;
    }
}
