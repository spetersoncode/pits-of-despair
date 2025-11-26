using PitsOfDespair.AI.Components;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that follows waypoints from a PatrolRouteComponent.
/// Uses ApproachGoal for pathfinding to each waypoint.
/// Automatically advances through waypoints as they are reached.
/// For individual/free-roaming patrol only - see LeaderPatrolGoal for pack leaders.
/// </summary>
public class PatrolRouteGoal : Goal
{
    private PatrolRouteComponent _routeComponent;

    public override bool IsFinished(AIContext context)
    {
        _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");

        return _routeComponent?.Route?.Type == PatrolRouteType.OneWay
            && _routeComponent.IsRouteComplete;
    }

    public override void TakeAction(AIContext context)
    {
        _routeComponent ??= context.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent");

        if (_routeComponent?.Route == null || _routeComponent.CurrentTarget == null)
        {
            Fail(context);
            return;
        }

        var currentTarget = _routeComponent.CurrentTarget;
        int waypointTolerance = _routeComponent.Route.WaypointTolerance;

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

        // Push ApproachGoal for current target
        var approach = new ApproachGoal(
            currentTarget.Value,
            desiredDistance: waypointTolerance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        var target = _routeComponent?.CurrentTarget;
        if (target != null)
        {
            return $"Patrol to {target.Value}";
        }
        return "Patrol Route";
    }
}
