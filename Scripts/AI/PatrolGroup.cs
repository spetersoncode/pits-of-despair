using System.Collections.Generic;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI;

/// <summary>
/// Shared patrol state for a group of creatures.
/// All members of a patrol group move toward the same waypoint together.
/// When any member reaches the waypoint, the group advances to the next one.
/// </summary>
public class PatrolGroup
{
    /// <summary>
    /// The shared patrol route for this group.
    /// </summary>
    public PatrolRoute Route { get; }

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
    /// Members of this patrol group.
    /// </summary>
    private readonly List<BaseEntity> _members = new();

    /// <summary>
    /// Creates a new patrol group with the specified route.
    /// </summary>
    public PatrolGroup(PatrolRoute route)
    {
        Route = route;
    }

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
    /// Adds a creature to this patrol group.
    /// </summary>
    public void AddMember(BaseEntity entity)
    {
        if (!_members.Contains(entity))
            _members.Add(entity);
    }

    /// <summary>
    /// Removes a creature from this patrol group.
    /// </summary>
    public void RemoveMember(BaseEntity entity)
    {
        _members.Remove(entity);
    }

    /// <summary>
    /// Gets all current members of the patrol group.
    /// </summary>
    public IReadOnlyList<BaseEntity> Members => _members;

    /// <summary>
    /// Checks if any member has reached the current waypoint.
    /// If so, advances to the next waypoint.
    /// </summary>
    public void CheckAndAdvance()
    {
        var target = CurrentTarget;
        if (target == null)
            return;

        int tolerance = Route?.WaypointTolerance ?? 1;

        foreach (var member in _members)
        {
            if (member == null || !Godot.GodotObject.IsInstanceValid(member))
                continue;

            int distance = DistanceHelper.ChebyshevDistance(member.GridPosition, target.Value);
            if (distance <= tolerance)
            {
                AdvanceWaypoint();
                return;
            }
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
