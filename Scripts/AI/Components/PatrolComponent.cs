using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.AI.Patrol;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Makes creature patrol to random distant locations when bored.
/// Responds to OnIAmBored to push PatrolGoal with configurable probability and distance.
/// Only creatures with this component will patrol.
/// </summary>
public partial class PatrolComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Chance to start patrolling each turn when bored (0.0 - 1.0).
    /// Higher values = more active patrolling, lower = more idle time.
    /// Default 0.3 gives creatures some downtime between patrols.
    /// </summary>
    [Export] public float PatrolChance { get; set; } = 0.3f;

    /// <summary>
    /// How far patrol routes extend from spawn location.
    /// Local = stay in room, Extended = include neighbors, Roaming = map-wide.
    /// Default Local for territorial/guard behavior.
    /// </summary>
    [Export] public PatrolScope Scope { get; set; } = PatrolScope.Local;

    /// <summary>
    /// Minimum distance for patrol waypoints.
    /// Ensures patrols cover meaningful ground rather than shuffling in place.
    /// Default 8 works well for most room sizes.
    /// </summary>
    [Export] public int MinDistance { get; set; } = 8;

    /// <summary>
    /// Maximum distance for patrol waypoints (Roaming scope only).
    /// Caps how far roaming creatures will wander from spawn.
    /// Default 40 allows cross-map patrols on most levels.
    /// </summary>
    [Export] public int MaxDistance { get; set; } = 40;

    /// <summary>
    /// Number of waypoints in patrol route.
    /// More waypoints = longer patrol loops, fewer = tighter patterns.
    /// Default 4 creates good coverage without excessive wandering.
    /// </summary>
    [Export] public int WaypointCount { get; set; } = 4;

    /// <summary>
    /// If true, this creature shares waypoint state with other grouped patrollers.
    /// All grouped creatures in an encounter move to the same waypoint together.
    /// Good for pack animals, squads. Default false for independent patrol.
    /// </summary>
    [Export] public bool Grouped { get; set; } = false;

    /// <summary>
    /// Shared patrol group for synchronized waypoint movement.
    /// Set by SpawnAIConfigurator when Grouped is true.
    /// </summary>
    public PatrolGroup PatrolGroup { get; set; }

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        // Don't patrol if we see enemies - combat takes precedence
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Random chance to patrol
        if (GD.Randf() < PatrolChance)
        {
            Goal patrolGoal;

            // Use route-based patrol if we have a patrol group or route component
            if (PatrolGroup != null || args.Context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent") != null)
            {
                patrolGoal = new PatrolRouteGoal(PatrolGroup);
            }
            else
            {
                patrolGoal = new PatrolGoal(MinDistance);
            }

            args.Context.AIComponent.GoalStack.Push(patrolGoal);
            args.Handled = true;
        }
    }
}
