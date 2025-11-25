namespace PitsOfDespair.AI.Patrol;

/// <summary>
/// Defines how far patrol routes extend from the spawn location.
/// </summary>
public enum PatrolScope
{
    /// <summary>
    /// Patrol within spawn region only. Good for guards, territorial creatures.
    /// Default behavior - creatures stay in their room.
    /// </summary>
    Local,

    /// <summary>
    /// Patrol spawn region plus adjacent connected regions.
    /// Good for sentries watching entrances/exits.
    /// </summary>
    Extended,

    /// <summary>
    /// Patrol across the map using random distant waypoints.
    /// Good for wandering hunters, roaming packs.
    /// </summary>
    Roaming
}
