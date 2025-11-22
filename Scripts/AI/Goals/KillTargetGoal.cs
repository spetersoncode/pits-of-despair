using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that pursues and attempts to kill a target entity.
/// Uses priority-based action selection:
/// 1. Melee attack (if in range)
/// 2. Defensive actions (healing, etc.)
/// 3. Ranged attack (if available)
/// 4. Item usage
/// 5. Movement (approach target)
///
/// Each action category gathers options from components via events,
/// then selects randomly from available actions weighted by priority.
/// </summary>
public class KillTargetGoal : Goal
{
    /// <summary>
    /// The entity we're trying to kill.
    /// </summary>
    public BaseEntity Target { get; private set; }

    public KillTargetGoal(BaseEntity target, Goal originalIntent = null)
    {
        Target = target;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        // Finished when target is gone or dead
        return Target == null || !GodotObject.IsInstanceValid(Target) || Target.IsDead;
    }

    public override void TakeAction(AIContext context)
    {
        // Priority-ordered action attempts
        if (TryMeleeAttack(context)) return;
        if (TryDefensiveAction(context)) return;
        if (TryRangedAttack(context)) return;
        if (TryUseItem(context)) return;
        if (TryMovement(context)) return;

        // All options exhausted - fail back to original intent
        Fail(context);
    }

    /// <summary>
    /// Attempts a melee attack if we're in range.
    /// Gathers melee actions from components and picks one randomly.
    /// </summary>
    private bool TryMeleeAttack(AIContext context)
    {
        // Only try if in melee range (distance <= 1)
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            Target.GridPosition);

        if (distance > 1)
        {
            return false;
        }

        // Gather melee actions from all components
        var args = new GetActionsEventArgs
        {
            Context = context,
            Target = Target
        };

        context.Entity.FireEvent(AIEvents.OnGetMeleeActions, args);

        if (args.ActionList.IsEmpty)
        {
            return false;
        }

        // Pick and execute random weighted action
        var action = args.ActionList.PickRandomWeighted();
        action?.Invoke(context);
        return true;
    }

    /// <summary>
    /// Attempts a defensive action (healing, blocking, etc.).
    /// </summary>
    private bool TryDefensiveAction(AIContext context)
    {
        var args = new GetActionsEventArgs
        {
            Context = context,
            Target = Target
        };

        context.Entity.FireEvent(AIEvents.OnGetDefensiveActions, args);

        if (args.ActionList.IsEmpty)
        {
            return false;
        }

        var action = args.ActionList.PickRandomWeighted();
        action?.Invoke(context);
        return true;
    }

    /// <summary>
    /// Attempts a ranged attack if we have ranged capabilities.
    /// </summary>
    private bool TryRangedAttack(AIContext context)
    {
        var args = new GetActionsEventArgs
        {
            Context = context,
            Target = Target
        };

        context.Entity.FireEvent(AIEvents.OnGetRangedActions, args);

        if (args.ActionList.IsEmpty)
        {
            return false;
        }

        var action = args.ActionList.PickRandomWeighted();
        action?.Invoke(context);

        // Fire success event for shoot-and-scoot behavior
        var successArgs = new GetActionsEventArgs
        {
            Context = context,
            Target = Target
        };
        context.Entity.FireEvent(AIEvents.OnRangedAttackSuccess, successArgs);

        return true;
    }

    /// <summary>
    /// Attempts to use an item (potions, scrolls, etc.).
    /// </summary>
    private bool TryUseItem(AIContext context)
    {
        var args = new GetActionsEventArgs
        {
            Context = context,
            Target = Target
        };

        context.Entity.FireEvent(AIEvents.OnGetItemActions, args);

        if (args.ActionList.IsEmpty)
        {
            return false;
        }

        var action = args.ActionList.PickRandomWeighted();
        action?.Invoke(context);
        return true;
    }

    /// <summary>
    /// Pushes an ApproachGoal to get closer to the target.
    /// This is the fallback when no other actions are available.
    /// </summary>
    private bool TryMovement(AIContext context)
    {
        // Push ApproachGoal to get closer to target
        var approach = new ApproachGoal(Target.GridPosition, desiredDistance: 1, originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
        return true;
    }

    public override string GetName() => $"Kill {Target?.DisplayName ?? "Unknown"}";
}
