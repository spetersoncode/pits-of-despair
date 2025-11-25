namespace PitsOfDespair.AI.Patrol;

/// <summary>
/// Defines how a patrol route handles reaching the final waypoint.
/// </summary>
public enum PatrolRouteType
{
    /// <summary>
    /// Return to first waypoint after reaching the last (circular path).
    /// </summary>
    Loop,

    /// <summary>
    /// Reverse direction at endpoints (back and forth).
    /// </summary>
    PingPong,

    /// <summary>
    /// Complete patrol after reaching the final waypoint.
    /// </summary>
    OneWay
}
