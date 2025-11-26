using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Attached to the pack leader. Manages patrol route, wait timing at waypoints,
/// and tracks followers. When leader dies, promotes a random follower.
/// </summary>
public partial class PackLeaderComponent : Node
{
    /// <summary>
    /// The patrol route for this pack.
    /// </summary>
    public PatrolRoute Route { get; set; }

    /// <summary>
    /// Current waypoint index in the route.
    /// </summary>
    public int CurrentWaypointIndex { get; set; } = 0;

    /// <summary>
    /// For PingPong routes: whether we're moving backwards through waypoints.
    /// </summary>
    public bool IsReversed { get; set; } = false;

    /// <summary>
    /// Whether a OneWay route has been completed.
    /// </summary>
    public bool IsRouteComplete { get; set; } = false;

    /// <summary>
    /// How many turns the leader waits at each waypoint for followers.
    /// </summary>
    [Export] public int WaitTurnsAtWaypoint { get; set; } = 4;

    /// <summary>
    /// Turns already waited at the current waypoint.
    /// Resets when advancing to next waypoint.
    /// </summary>
    public int TurnsWaitedAtCurrent { get; set; } = 0;

    /// <summary>
    /// Whether the leader is currently waiting at a waypoint.
    /// </summary>
    public bool IsWaiting { get; set; } = false;

    private readonly List<BaseEntity> _followers = new();

    /// <summary>
    /// All followers in this pack.
    /// </summary>
    public IReadOnlyList<BaseEntity> Followers => _followers;

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

    public override void _Ready()
    {
        // Connect to leader's death signal
        var entity = GetParent<BaseEntity>();
        if (entity != null)
        {
            var healthComp = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (healthComp != null)
            {
                healthComp.Connect(HealthComponent.SignalName.Died, Callable.From(OnLeaderDied));
            }
        }
    }

    public override void _ExitTree()
    {
        // Cleanup death signal connection
        var entity = GetParent<BaseEntity>();
        if (entity != null && IsInstanceValid(entity))
        {
            var healthComp = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (healthComp != null && IsInstanceValid(healthComp))
            {
                if (healthComp.IsConnected(HealthComponent.SignalName.Died, Callable.From(OnLeaderDied)))
                {
                    healthComp.Disconnect(HealthComponent.SignalName.Died, Callable.From(OnLeaderDied));
                }
            }
        }
    }

    /// <summary>
    /// Adds a follower to this pack.
    /// </summary>
    public void AddFollower(BaseEntity follower)
    {
        if (!_followers.Contains(follower))
            _followers.Add(follower);
    }

    /// <summary>
    /// Removes a follower from this pack.
    /// </summary>
    public void RemoveFollower(BaseEntity follower)
    {
        _followers.Remove(follower);
    }

    /// <summary>
    /// Called each turn when leader is at waypoint. Returns true when done waiting.
    /// </summary>
    public bool TickWait()
    {
        TurnsWaitedAtCurrent++;
        if (TurnsWaitedAtCurrent >= WaitTurnsAtWaypoint)
        {
            IsWaiting = false;
            TurnsWaitedAtCurrent = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks that we've arrived at a waypoint and should start waiting.
    /// </summary>
    public void StartWaiting()
    {
        IsWaiting = true;
        TurnsWaitedAtCurrent = 0;
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
        TurnsWaitedAtCurrent = 0;
        IsWaiting = false;

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
    /// Called when the leader dies. Promotes a random follower to become the new leader.
    /// </summary>
    private void OnLeaderDied()
    {
        // Get living followers
        var livingFollowers = _followers
            .Where(f => f != null && IsInstanceValid(f))
            .ToList();

        if (livingFollowers.Count == 0)
        {
            GD.Print("[PackLeader] Leader died with no followers - pack dissolved");
            return;
        }

        // Randomly select new leader
        var newLeader = livingFollowers[GD.RandRange(0, livingFollowers.Count - 1)];
        GD.Print($"[PackLeader] Leader died, promoting {newLeader.DisplayName} to leader");

        // Create new PackLeaderComponent for new leader, copying route state
        var newLeaderComp = new PackLeaderComponent
        {
            Route = Route,
            CurrentWaypointIndex = CurrentWaypointIndex,
            IsReversed = IsReversed,
            IsRouteComplete = IsRouteComplete,
            WaitTurnsAtWaypoint = WaitTurnsAtWaypoint,
            TurnsWaitedAtCurrent = 0,
            IsWaiting = false
        };
        newLeaderComp.Name = "PackLeaderComponent";

        // Remove the follower's PackFollowerComponent
        var oldFollowerComp = newLeader.GetNodeOrNull<PackFollowerComponent>("PackFollowerComponent");
        if (oldFollowerComp != null)
        {
            oldFollowerComp.QueueFree();
        }

        // Add leader component to new leader
        newLeader.AddChild(newLeaderComp);

        // Transfer remaining followers to new leader
        foreach (var follower in livingFollowers)
        {
            if (follower == newLeader)
                continue;

            newLeaderComp.AddFollower(follower);

            // Update follower's reference to new leader
            var followerComp = follower.GetNodeOrNull<PackFollowerComponent>("PackFollowerComponent");
            if (followerComp != null)
            {
                followerComp.Leader = newLeader;
            }
        }
    }
}
