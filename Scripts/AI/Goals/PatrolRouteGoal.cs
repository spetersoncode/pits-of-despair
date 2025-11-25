using PitsOfDespair.AI.Components;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that follows waypoints from a PatrolRouteComponent.
/// Uses ApproachGoal for pathfinding to each waypoint.
/// Automatically advances through waypoints as they are reached.
/// </summary>
public class PatrolRouteGoal : Goal
{
    private PatrolRouteComponent _routeComponent;

    public override bool IsFinished(AIContext context)
    {
        // Ensure we have the route component
        _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");

        // Finished only if OneWay route and complete
        return _routeComponent?.Route?.Type == PatrolRouteType.OneWay
            && _routeComponent.IsRouteComplete;
    }

    public override void TakeAction(AIContext context)
    {
        // Get route component from entity
        _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");

        if (_routeComponent?.Route == null || _routeComponent.CurrentTarget == null)
        {
            Fail(context);
            return;
        }

        var currentTarget = _routeComponent.CurrentTarget.Value;

        // Check if at current waypoint
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            currentTarget);

        if (distance <= _routeComponent.Route.WaypointTolerance)
        {
            _routeComponent.AdvanceWaypoint();

            // If route just completed (OneWay), we're done
            if (_routeComponent.IsRouteComplete)
                return;

            // Update target for next waypoint
            if (_routeComponent.CurrentTarget == null)
            {
                Fail(context);
                return;
            }
            currentTarget = _routeComponent.CurrentTarget.Value;
        }

        // Push ApproachGoal for current target
        var approach = new ApproachGoal(
            currentTarget,
            desiredDistance: _routeComponent.Route.WaypointTolerance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        if (_routeComponent?.CurrentTarget != null)
        {
            return $"Patrol to {_routeComponent.CurrentTarget.Value}";
        }
        return "Patrol Route";
    }
}
