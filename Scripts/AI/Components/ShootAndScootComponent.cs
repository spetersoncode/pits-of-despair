using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// AI component that causes creatures to retreat after successful ranged attacks.
/// Responds to OnRangedAttackSuccess by pushing a FleeGoal onto the stack.
/// This creates "kiting" behavior for ranged creatures.
///
/// Optimizations:
/// - Only triggers when target is within threat range (not when already safe)
/// - Skips fleeing if target is nearly dead (finish them off instead)
/// </summary>
public partial class ShootAndScootComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Number of turns to flee after a successful ranged attack.
    /// </summary>
    [Export] public int FleeTurns { get; set; } = 3;

    /// <summary>
    /// Distance to try to maintain from the target.
    /// </summary>
    [Export] public int FleeDistance { get; set; } = 4;

    /// <summary>
    /// Only trigger flee if target is within this distance.
    /// If target is farther away, no need to flee - they're not a threat yet.
    /// </summary>
    [Export] public int TriggerDistance { get; set; } = 3;

    /// <summary>
    /// If target HP is at or below this threshold, don't flee - finish them off.
    /// Set to 0 to disable this behavior.
    /// </summary>
    [Export] public int FinishThreshold { get; set; } = 8;

    /// <summary>
    /// Handle AI events - responds to OnRangedAttackSuccess.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnRangedAttackSuccess)
        {
            return;
        }

        if (args.Target == null || args.Context?.AIComponent == null)
        {
            return;
        }

        var entity = args.Context.Entity;
        var target = args.Target;

        // Check distance - only flee if target is close enough to be a threat
        int distance = DistanceHelper.ChebyshevDistance(
            entity.GridPosition,
            target.GridPosition);

        if (distance >= FleeDistance)
        {
            // Already at safe distance, no need to flee
            return;
        }

        if (distance > TriggerDistance)
        {
            // Target not close enough to be threatening, keep shooting
            return;
        }

        // Check if target is nearly dead - finish them off instead of fleeing
        if (FinishThreshold > 0)
        {
            var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (targetHealth != null && targetHealth.CurrentHealth <= FinishThreshold)
            {
                // Target is low HP, stay and finish them
                return;
            }
        }

        var ai = args.Context.AIComponent;
        var currentGoal = ai.GoalStack.Peek();

        // Push flee goal onto stack - will execute before returning to combat
        var fleeGoal = new FleeGoal(
            target,
            FleeTurns,
            FleeDistance,
            originalIntent: currentGoal);

        ai.GoalStack.Push(fleeGoal);
    }
}
