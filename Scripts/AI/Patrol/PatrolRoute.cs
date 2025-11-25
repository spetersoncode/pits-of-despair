using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.AI.Patrol;

/// <summary>
/// Immutable data class representing a patrol route.
/// Contains ordered waypoints and behavior configuration.
/// </summary>
public class PatrolRoute
{
    /// <summary>
    /// Ordered list of waypoint positions to visit.
    /// </summary>
    public IReadOnlyList<GridPosition> Waypoints { get; }

    /// <summary>
    /// How the route handles reaching the final waypoint.
    /// </summary>
    public PatrolRouteType Type { get; }

    /// <summary>
    /// Distance (Chebyshev) at which a waypoint is considered "reached".
    /// </summary>
    public int WaypointTolerance { get; }

    /// <summary>
    /// Creates a new patrol route.
    /// </summary>
    /// <param name="waypoints">Ordered waypoint positions.</param>
    /// <param name="type">Route loop behavior (default: Loop).</param>
    /// <param name="waypointTolerance">Distance to consider waypoint reached (default: 1).</param>
    public PatrolRoute(
        IEnumerable<GridPosition> waypoints,
        PatrolRouteType type = PatrolRouteType.Loop,
        int waypointTolerance = 1)
    {
        Waypoints = new List<GridPosition>(waypoints).AsReadOnly();
        Type = type;
        WaypointTolerance = waypointTolerance;
    }
}
