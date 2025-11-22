namespace PitsOfDespair.AI;

/// <summary>
/// Defines movement and navigation capabilities for a creature.
/// Used by NavigationWeightMap to calculate appropriate pathfinding costs.
/// </summary>
public class CreatureCapabilities
{
    /// <summary>
    /// Whether the creature is intelligent enough to open doors and use complex pathing.
    /// Unintelligent creatures treat closed doors as impassable.
    /// </summary>
    public bool IsIntelligent { get; set; } = true;

    /// <summary>
    /// Whether the creature can fly over ground-based hazards and obstacles.
    /// Flying creatures ignore ground hazards like webs and shallow water.
    /// </summary>
    public bool CanFly { get; set; } = false;

    /// <summary>
    /// Whether the creature can burrow through walls.
    /// Burrowing creatures can path through walls at high cost (20) instead of impassable.
    /// </summary>
    public bool CanBurrow { get; set; } = false;

    /// <summary>
    /// Default capabilities for most creatures (intelligent, cannot fly or burrow).
    /// </summary>
    public static CreatureCapabilities Default => new();

    /// <summary>
    /// Extracts capabilities from an entity.
    /// Currently returns defaults; will be extended when CreatureData supports capabilities.
    /// </summary>
    public static CreatureCapabilities FromEntity(Entities.BaseEntity entity)
    {
        // TODO: Extract from CreatureData when capability fields are added
        // var creatureData = entity.GetNodeOrNull<CreatureData>("CreatureData");
        // if (creatureData != null)
        // {
        //     return new CreatureCapabilities
        //     {
        //         IsIntelligent = creatureData.IsIntelligent,
        //         CanFly = creatureData.CanFly,
        //         CanBurrow = creatureData.CanBurrow
        //     };
        // }

        return Default;
    }
}
