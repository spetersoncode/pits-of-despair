using Godot;
using PitsOfDespair.AI.Goals;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// IAIEventHandler that pushes PatrolRouteGoal when the creature is bored.
/// Requires a PatrolRouteComponent sibling with a valid route.
/// </summary>
public partial class PatrolRouteInjector : Node, IAIEventHandler
{
    /// <summary>
    /// Chance to start patrolling each turn when bored (0.0 - 1.0).
    /// </summary>
    [Export] public float PatrolChance { get; set; } = 0.7f;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        // Don't patrol if we see enemies - combat takes precedence
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Check for valid patrol route component
        var routeComponent = GetParent()?.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");
        if (routeComponent?.Route == null || routeComponent.Route.Waypoints.Count < 2)
            return;

        // Skip if route is complete (OneWay)
        if (routeComponent.IsRouteComplete)
            return;

        // Random chance to patrol
        if (GD.Randf() < PatrolChance)
        {
            var patrolGoal = new PatrolRouteGoal();
            args.Context.AIComponent.GoalStack.Push(patrolGoal);
            args.Handled = true;
        }
    }
}
