namespace PitsOfDespair.Scripts.Skills;

/// <summary>
/// Defines how a skill selects its targets.
/// </summary>
public enum TargetingType
{
    /// <summary>
    /// Targets the caster. No selection needed.
    /// </summary>
    Self,

    /// <summary>
    /// Targets one of the 8 adjacent tiles.
    /// </summary>
    Adjacent,

    /// <summary>
    /// Targets any tile within range.
    /// </summary>
    Tile,

    /// <summary>
    /// Targets an enemy entity within range.
    /// </summary>
    Enemy,

    /// <summary>
    /// Targets an allied entity within range.
    /// </summary>
    Ally,

    /// <summary>
    /// Targets a center point, affecting all entities within a radius.
    /// </summary>
    Area,

    /// <summary>
    /// Targets a direction, affecting all tiles in a line.
    /// </summary>
    Line,

    /// <summary>
    /// Targets a direction, affecting tiles in a cone pattern.
    /// </summary>
    Cone
}
