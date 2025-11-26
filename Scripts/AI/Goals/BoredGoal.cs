using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// The fallback goal that sits at the bottom of the goal stack.
/// Never finishes - always available as a fallback when other goals complete.
///
/// <para><b>Architecture: Event-First Design</b></para>
///
/// This goal fires OnIAmBored BEFORE checking for enemies. This ordering is critical
/// and intentional - it allows AI components to override default combat behavior.
///
/// <para><b>Why OnIAmBored fires first:</b></para>
/// <list type="bullet">
///   <item>CowardlyComponent can push FleeGoal instead of fighting</item>
///   <item>FollowLeaderComponent can prioritize staying with the group</item>
///   <item>Any component can intercept and override the default "see enemy = attack" behavior</item>
/// </list>
///
/// <para><b>How non-combat components coexist:</b></para>
/// Components like PatrolComponent and WanderingComponent check for visible enemies
/// internally and return early (without setting Handled=true) when enemies are present.
/// This lets them defer to the combat check that follows.
///
/// <para><b>Behavior priority:</b></para>
/// <list type="number">
///   <item>Fire OnIAmBored - components can push goals and set Handled=true to override</item>
///   <item>If not handled, check for visible enemies and push KillTargetGoal</item>
///   <item>If no enemies, do nothing (wait in place)</item>
/// </list>
/// </summary>
public class BoredGoal : Goal
{
    public override bool IsFinished(AIContext context)
    {
        // Never finished - always at bottom of stack as the ultimate fallback
        return false;
    }

    public override void TakeAction(AIContext context)
    {
        // ============================================================================
        // STEP 1: Fire OnIAmBored event FIRST
        // ============================================================================
        // This MUST happen before the enemy check. Components respond to this event
        // to inject custom behavior:
        //
        // - CowardlyComponent: Sees enemies, pushes FleeGoal, sets Handled=true
        //   → Creature flees instead of fighting
        //
        // - PatrolComponent: Checks for enemies internally, returns if any visible
        //   → Does NOT set Handled, so combat check below will trigger
        //
        // - WanderingComponent: Same pattern - defers to combat when enemies present
        //
        // - FollowLeaderComponent: May push FollowEntityGoal to stay with leader
        //
        // The key insight: components that WANT to override combat set Handled=true.
        // Components that want to defer to combat simply return without setting it.
        // ============================================================================
        var args = new GetActionsEventArgs { Context = context };
        context.Entity.FireEvent(AIEvents.OnIAmBored, args);

        if (args.Handled) return;

        // ============================================================================
        // STEP 2: Default combat behavior (only if no component handled the event)
        // ============================================================================
        // Standard aggressive behavior: see enemy → attack enemy.
        // This is the fallback for creatures without special behavioral components,
        // or when components like PatrolComponent deferred because enemies are visible.
        // ============================================================================
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

        // ============================================================================
        // STEP 3: Nothing to do - wait in place
        // ============================================================================
        // No components pushed a goal, no enemies visible. Creature idles.
        // ============================================================================
    }

    private void PushCombatGoal(AIContext context, BaseEntity target)
    {
        var killGoal = new KillTargetGoal(target, originalIntent: this);
        context.AIComponent.GoalStack.Push(killGoal);
    }

    public override string GetName() => "Bored";
}
