using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// The fallback goal that sits at the bottom of the goal stack.
/// Never finishes - always available as a fallback when other goals complete.
///
/// Behavior priority:
/// 1. Find visible enemies and push combat goal (combat first!)
/// 2. Fire OnIAmBored event - let components inject idle behavior (patrol, wander, follow, etc.)
/// 3. Do nothing (wait)
/// </summary>
public class BoredGoal : Goal
{
    public override bool IsFinished(AIContext context)
    {
        // Never finished - always at bottom of stack
        return false;
    }

    public override void TakeAction(AIContext context)
    {
        // Priority 1: Combat - always attack visible enemies first
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

        // Priority 2: Idle behaviors (patrol, wander, follow, etc.)
        var args = new GetActionsEventArgs { Context = context };
        context.Entity.FireEvent(AIEvents.OnIAmBored, args);

        // If a component handled it (pushed a goal), we're done
        if (args.Handled) return;

        // Otherwise: do nothing (wait in place)
    }

    private void PushCombatGoal(AIContext context, BaseEntity target)
    {
        var killGoal = new KillTargetGoal(target, originalIntent: this);
        context.AIComponent.GoalStack.Push(killGoal);
    }

    public override string GetName() => "Bored";
}
