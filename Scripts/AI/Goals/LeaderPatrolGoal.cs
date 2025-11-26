using Godot;
using PitsOfDespair.AI.Components;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for pack leaders. Navigates through patrol waypoints, waiting at each
/// for a configured number of turns to let followers catch up.
/// </summary>
public class LeaderPatrolGoal : Goal
{
    private PackLeaderComponent _leaderComp;

    public override bool IsFinished(AIContext context)
    {
        _leaderComp ??= context.Entity.GetNodeOrNull<PackLeaderComponent>("PackLeaderComponent");

        if (_leaderComp == null || !GodotObject.IsInstanceValid(_leaderComp))
            return true;

        // Finished if OneWay route is complete
        return _leaderComp.Route?.Type == PatrolRouteType.OneWay
            && _leaderComp.IsRouteComplete;
    }

    public override void TakeAction(AIContext context)
    {
        // Abort patrol if we see enemies - combat takes priority
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            Fail(context);
            return;
        }

        _leaderComp ??= context.Entity.GetNodeOrNull<PackLeaderComponent>("PackLeaderComponent");

        if (_leaderComp == null || _leaderComp.Route == null)
        {
            Fail(context);
            return;
        }

        var currentTarget = _leaderComp.CurrentTarget;
        if (currentTarget == null)
        {
            Fail(context);
            return;
        }

        int waypointTolerance = _leaderComp.Route.WaypointTolerance;

        // Check if at current waypoint
        int distance = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            currentTarget.Value);

        if (distance <= waypointTolerance)
        {
            // We're at the waypoint
            if (!_leaderComp.IsWaiting)
            {
                // Just arrived - start waiting
                _leaderComp.StartWaiting();
                return; // Skip this turn (we arrived)
            }

            // Already waiting - tick the counter
            if (_leaderComp.TickWait())
            {
                // Done waiting - advance to next waypoint
                _leaderComp.AdvanceWaypoint();

                if (_leaderComp.IsRouteComplete)
                    return;

                currentTarget = _leaderComp.CurrentTarget;
                if (currentTarget == null)
                {
                    Fail(context);
                    return;
                }
            }
            else
            {
                // Still waiting - stay in place
                return;
            }
        }

        // Not at waypoint - move toward it
        var approach = new ApproachGoal(
            currentTarget.Value,
            desiredDistance: waypointTolerance,
            originalIntent: this);
        context.AIComponent.GoalStack.Push(approach);
    }

    public override string GetName()
    {
        if (_leaderComp?.IsWaiting == true)
        {
            return $"Waiting at waypoint ({_leaderComp.TurnsWaitedAtCurrent}/{_leaderComp.WaitTurnsAtWaypoint})";
        }

        var target = _leaderComp?.CurrentTarget;
        if (target != null)
        {
            return $"Leading patrol to {target.Value}";
        }
        return "Leading patrol";
    }
}
