using Godot;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Node component that stores patrol route state for an entity.
/// Tracks current progress through waypoints and handles route traversal logic.
/// </summary>
public partial class PatrolRouteComponent : Node
{
    /// <summary>
    /// The patrol route data.
    /// </summary>
    public PatrolRoute Route { get; set; }

    /// <summary>
    /// Current waypoint index in the route.
    /// </summary>
    public int CurrentWaypointIndex { get; private set; } = 0;

    /// <summary>
    /// For PingPong routes: whether we're moving backwards through waypoints.
    /// </summary>
    public bool IsReversed { get; private set; } = false;

    /// <summary>
    /// Whether a OneWay route has been completed.
    /// </summary>
    public bool IsRouteComplete { get; private set; } = false;

    /// <summary>
    /// Gets the current target waypoint position.
    /// Returns null if route is not set or is complete.
    /// </summary>
    public GridPosition? CurrentTarget
    {
        get
        {
            if (Route == null || Route.Waypoints.Count == 0 || IsRouteComplete)
                return null;

            return Route.Waypoints[CurrentWaypointIndex];
        }
    }

    /// <summary>
    /// Advances to the next waypoint in the route.
    /// Handles Loop, PingPong, and OneWay behaviors.
    /// </summary>
    public void AdvanceWaypoint()
    {
        if (Route == null || Route.Waypoints.Count == 0 || IsRouteComplete)
            return;

        int waypointCount = Route.Waypoints.Count;

        switch (Route.Type)
        {
            case PatrolRouteType.Loop:
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypointCount;
                break;

            case PatrolRouteType.PingPong:
                if (IsReversed)
                {
                    CurrentWaypointIndex--;
                    if (CurrentWaypointIndex <= 0)
                    {
                        CurrentWaypointIndex = 0;
                        IsReversed = false;
                    }
                }
                else
                {
                    CurrentWaypointIndex++;
                    if (CurrentWaypointIndex >= waypointCount - 1)
                    {
                        CurrentWaypointIndex = waypointCount - 1;
                        IsReversed = true;
                    }
                }
                break;

            case PatrolRouteType.OneWay:
                CurrentWaypointIndex++;
                if (CurrentWaypointIndex >= waypointCount)
                {
                    CurrentWaypointIndex = waypointCount - 1;
                    IsRouteComplete = true;
                }
                break;
        }
    }

    /// <summary>
    /// Resets patrol progress to the beginning.
    /// </summary>
    public void ResetProgress()
    {
        CurrentWaypointIndex = 0;
        IsReversed = false;
        IsRouteComplete = false;
    }
}
