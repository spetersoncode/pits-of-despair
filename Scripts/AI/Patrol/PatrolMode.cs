namespace PitsOfDespair.AI.Patrol;

/// <summary>
/// Defines how grouped patrols coordinate movement.
/// </summary>
public enum PatrolMode
{
    /// <summary>
    /// Each creature manages their own waypoints independently.
    /// No synchronization with other creatures in the group.
    /// </summary>
    FreeRoaming,

    /// <summary>
    /// Leader navigates waypoints; followers pursue the leader.
    /// Leader waits N turns at each waypoint for followers to catch up.
    /// If leader dies, a follower is promoted to leader.
    /// </summary>
    LeaderPack
}
