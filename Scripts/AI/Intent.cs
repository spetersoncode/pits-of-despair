namespace PitsOfDespair.AI;

/// <summary>
/// Player-facing AI state that describes what a creature is currently doing.
/// Used for UI display and tactical decision-making.
/// </summary>
public enum Intent
{
    /// <summary>
    /// Creature is inactive and won't act until woken.
    /// Display: Gray, relaxed posture.
    /// </summary>
    Sleeping,

    /// <summary>
    /// No current goal, may wander.
    /// Display: White, neutral.
    /// </summary>
    Idle,

    /// <summary>
    /// Following a patrol route.
    /// Display: Blue, moving purposefully.
    /// </summary>
    Patrolling,

    /// <summary>
    /// Holding position, watching for threats.
    /// Display: Yellow, alert stance.
    /// </summary>
    Guarding,

    /// <summary>
    /// Actively pursuing a target.
    /// Display: Orange, aggressive.
    /// </summary>
    Hunting,

    /// <summary>
    /// In combat, attacking.
    /// Display: Red, combat stance.
    /// </summary>
    Attacking,

    /// <summary>
    /// Running away from a threat.
    /// Display: Purple, fleeing.
    /// </summary>
    Fleeing,

    /// <summary>
    /// Following a leader or ally.
    /// Display: Cyan, following.
    /// </summary>
    Following,

    /// <summary>
    /// Searching for items or resources.
    /// Display: Green, scavenging.
    /// </summary>
    Scavenging,

    /// <summary>
    /// Wandering aimlessly.
    /// Display: Gray-blue, wandering.
    /// </summary>
    Wandering
}
