using PitsOfDespair.AI.Components;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that follows waypoints from a PatrolRouteComponent or shared PatrolGroup.
/// Uses ApproachGoal for pathfinding to each waypoint.
/// Automatically advances through waypoints as they are reached.
/// When using a PatrolGroup, all members share the same current waypoint.
/// </summary>
public class PatrolRouteGoal : Goal
{
    private PatrolRouteComponent _routeComponent;
    private PatrolGroup _patrolGroup;

    /// <summary>
    /// Creates a patrol route goal.
    /// </summary>
    /// <param name="patrolGroup">Optional shared patrol group for synchronized movement.</param>
    public PatrolRouteGoal(PatrolGroup patrolGroup = null)
    {
        _patrolGroup = patrolGroup;
    }

    public override bool IsFinished(AIContext context)
    {
        // Check group first
        if (_patrolGroup != null)
        {
            return _patrolGroup.Route?.Type == PatrolRouteType.OneWay
                && _patrolGroup.IsRouteComplete;
        }

        // Fall back to individual route component
        _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");
        return _routeComponent?.Route?.Type == PatrolRouteType.OneWay
            && _routeComponent.IsRouteComplete;
    }

    public override void TakeAction(AIContext context)
    {
        // Get current target from group or individual component
        GridPosition? currentTarget;
        int waypointTolerance;

        if (_patrolGroup != null)
        {
            currentTarget = _patrolGroup.CurrentTarget;
            waypointTolerance = _patrolGroup.Route?.WaypointTolerance ?? 1;

            if (currentTarget == null)
            {
                Fail(context);
                return;
            }

            // Check if at current waypoint - advance the group (syncs all members)
            int distance = DistanceHelper.ChebyshevDistance(
                context.Entity.GridPosition,
                currentTarget.Value);

            if (distance <= waypointTolerance)
            {
                _patrolGroup.AdvanceWaypoint();

                if (_patrolGroup.IsRouteComplete)
                    return;

                currentTarget = _patrolGroup.CurrentTarget;
                if (currentTarget == null)
                {
                    Fail(context);
                    return;
                }
            }
        }
        else
        {
            // Use individual route component
            _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");

            if (_routeComponent?.Route == null || _routeComponent.CurrentTarget == null)
            {
                Fail(context);
                return;
            }

            currentTarget = _routeComponent.CurrentTarget;
            waypointTolerance = _routeComponent.Route.WaypointTolerance;

            // Check if at current waypoint
            int distance = DistanceHelper.ChebyshevDistance(
                context.Entity.GridPosition,
                currentTarget.Value);

            if (distance <= waypointTolerance)
            {
                _routeComponent.AdvanceWaypoint();

                if (_routeComponent.IsRouteComplete)
                    return;

                currentTarget = _routeComponent.CurrentTarget;
                if (currentTarget == null)
                {
                    Fail(context);
                    return;
                }
            }
        }

        // Push ApproachGoal for current target
        var approach = new ApproachGoal(
            currentTarget.Value,
            desiredDistance: waypointTolerance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        GridPosition? target = _patrolGroup?.CurrentTarget ?? _routeComponent?.CurrentTarget;
        if (target != null)
        {
            return $"Patrol to {target.Value}";
        }
        return "Patrol Route";
    }
}
