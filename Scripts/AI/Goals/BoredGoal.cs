using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// The fallback goal that sits at the bottom of the goal stack.
/// Never finishes - always available as a fallback when other goals complete.
///
/// Behavior priority:
/// 1. Fire OnIAmBored event - let components inject behavior (flee, defend, etc.)
/// 2. If has protection target: follow/defend VIP
/// 3. Find visible enemies and push combat goal
/// 4. Random chance to wander
/// 5. Do nothing (wait)
/// </summary>
public class BoredGoal : Goal
{
    private const float WanderChance = 0.3f;

    public override bool IsFinished(AIContext context)
    {
        // Never finished - always at bottom of stack
        return false;
    }

    public override void TakeAction(AIContext context)
    {
        // Fire event to let components inject behavior (flee, defend, etc.)
        var args = new GetActionsEventArgs { Context = context };
        context.Entity.FireEvent(AIEvents.OnIAmBored, args);

        // If a component handled it (pushed a goal), we're done
        if (args.Handled) return;

        // Check for protection target (friendly bodyguard behavior)
        if (TryProtectionBehavior(context))
            return;

        // Default: try to find an enemy to attack
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            var target = context.GetClosestEnemy(enemies);
            if (target != null)
            {
                PushCombatGoal(context, target);
                return;
            }
        }

        // Fallback: random chance to wander
        if (GD.Randf() < WanderChance)
        {
            var wanderGoal = new WanderGoal(originalIntent: this);
            context.AIComponent.GoalStack.Push(wanderGoal);
            return;
        }

        // Otherwise: do nothing (wait in place)
    }

    /// <summary>
    /// Handles protection target behavior for friendly creatures.
    /// Priority: 1) Stay near VIP, 2) Defend against threats (VIP or self can see)
    /// </summary>
    private bool TryProtectionBehavior(AIContext context)
    {
        var target = context.ProtectionTarget;
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            target.GridPosition);

        int followDistance = context.AIComponent.FollowDistance;

        // Priority 1: If too far from VIP, follow them
        if (distance > followDistance)
        {
            var followGoal = new FollowTargetGoal(originalIntent: this);
            context.AIComponent.GoalStack.Push(followGoal);
            return true;
        }

        // Priority 2: If VIP or protector can see enemies, defend
        var vipThreats = context.GetEnemiesVisibleToEntity(target);
        var protectorThreats = context.GetVisibleEnemies();
        if (vipThreats.Count > 0 || protectorThreats.Count > 0)
        {
            var defendGoal = new DefendTargetGoal(originalIntent: this);
            context.AIComponent.GoalStack.Push(defendGoal);
            return true;
        }

        // Near VIP with no threats - don't handle, fall through to normal behavior
        return false;
    }

    private void PushCombatGoal(AIContext context, BaseEntity target)
    {
        var killGoal = new KillTargetGoal(target, originalIntent: this);
        context.AIComponent.GoalStack.Push(killGoal);
    }

    public override string GetName() => "Bored";
}
