using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// The fallback goal that sits at the bottom of the goal stack.
/// Never finishes - always available as a fallback when other goals complete.
///
/// Behavior priority:
/// 1. Fire OnIAmBored event - let components inject behavior (flee, defend, etc.)
/// 2. Find visible enemies and push combat goal
/// 3. Random chance to wander
/// 4. Do nothing (wait)
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

    private void PushCombatGoal(AIContext context, BaseEntity target)
    {
        var killGoal = new KillTargetGoal(target, originalIntent: this);
        context.AIComponent.GoalStack.Push(killGoal);
    }

    public override string GetName() => "Bored";
}
