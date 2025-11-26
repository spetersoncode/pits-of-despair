using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.AI.Patrol;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Makes creature patrol to random distant locations when bored.
/// Responds to OnIAmBored to push patrol goals with configurable probability and distance.
/// Supports two modes: FreeRoaming (individual) and LeaderPack (leader-follower).
/// </summary>
public partial class PatrolComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Patrol coordination mode.
    /// FreeRoaming: Each creature patrols independently.
    /// LeaderPack: Leader navigates waypoints, followers pursue leader.
    /// </summary>
    [Export] public PatrolMode Mode { get; set; } = PatrolMode.FreeRoaming;

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
    /// How many turns the leader waits at each waypoint (LeaderPack mode only).
    /// Gives followers time to catch up before moving to next waypoint.
    /// </summary>
    [Export] public int WaitTurns { get; set; } = 4;

    /// <summary>
    /// How close followers stay to the leader (LeaderPack mode only).
    /// Measured in tiles (Chebyshev distance).
    /// </summary>
    [Export] public int FollowDistance { get; set; } = 2;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        // Don't patrol if we see enemies - combat takes precedence
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Random chance to patrol
        if (GD.Randf() >= PatrolChance)
            return;

        Goal patrolGoal = Mode switch
        {
            PatrolMode.LeaderPack => CreateLeaderPackGoal(args.Context),
            _ => CreateFreeRoamGoal(args.Context)
        };

        if (patrolGoal != null)
        {
            args.Context.AIComponent.GoalStack.Push(patrolGoal);
            args.Handled = true;
        }
    }

    private Goal CreateFreeRoamGoal(AIContext context)
    {
        // Use route-based patrol if we have a route component
        if (context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent") != null)
        {
            return new PatrolRouteGoal();
        }

        // Fall back to simple random patrol (for Local scope without pre-generated route)
        return new PatrolGoal(MinDistance);
    }

    private Goal CreateLeaderPackGoal(AIContext context)
    {
        // Check if we're the leader
        var leaderComp = context.Entity.GetNodeOrNull<PackLeaderComponent>("PackLeaderComponent");
        if (leaderComp != null)
        {
            return new LeaderPatrolGoal();
        }

        // Check if we're a follower
        var followerComp = context.Entity.GetNodeOrNull<PackFollowerComponent>("PackFollowerComponent");
        if (followerComp != null)
        {
            return new FollowPackLeaderGoal();
        }

        // No pack role assigned - fall back to free roam
        GD.PushWarning($"[PatrolComponent] {context.Entity.DisplayName} has LeaderPack mode but no pack role - falling back to free roam");
        return CreateFreeRoamGoal(context);
    }
}
