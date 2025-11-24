namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Types of visual effects that can be spawned.
/// </summary>
public enum VisualEffectType
{
    /// <summary>
    /// Expanding rings explosion effect (fire, magic, etc.)
    /// </summary>
    Explosion,

    /// <summary>
    /// Healing glow effect (future)
    /// </summary>
    Heal,

    /// <summary>
    /// Teleportation shimmer effect (future)
    /// </summary>
    Teleport,

    /// <summary>
    /// Impact flash effect (future)
    /// </summary>
    Impact,

    /// <summary>
    /// Line/beam effect traveling from origin to target (tunneling, lightning, etc.)
    /// </summary>
    Beam
}
