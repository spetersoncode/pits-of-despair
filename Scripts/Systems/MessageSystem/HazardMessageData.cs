using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Holds hazard damage data for combining with death messages.
/// </summary>
public class HazardMessageData
{
    /// <summary>
    /// The entity that took damage.
    /// </summary>
    public BaseEntity Target { get; set; }

    /// <summary>
    /// The amount of damage dealt.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The hazard type (e.g., "poison_cloud").
    /// </summary>
    public string HazardType { get; set; }

    /// <summary>
    /// Whether the target died from this damage.
    /// </summary>
    public bool TargetDied { get; set; }

    /// <summary>
    /// XP reward if the target died (player always gets credit for their hazards).
    /// </summary>
    public int XPReward { get; set; }
}
