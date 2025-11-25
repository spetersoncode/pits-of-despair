namespace PitsOfDespair.Targeting;

/// <summary>
/// Defines how a targeting source (skill, item, weapon) selects its targets.
/// Each type maps to a specific handler.
/// </summary>
public enum TargetingType
{
    /// <summary>
    /// Targets creatures (entities with health) within range.
    /// Uses Filter to determine valid targets (Enemy/Ally/Creature).
    /// </summary>
    Creature,

    /// <summary>
    /// Targets any walkable tile within range.
    /// </summary>
    Tile,

    /// <summary>
    /// Targets a center point, affecting all entities within a radius.
    /// </summary>
    Area,

    /// <summary>
    /// Targets a direction, affecting all tiles in a line from caster.
    /// </summary>
    Line,

    /// <summary>
    /// Targets a direction, affecting tiles in a cone pattern.
    /// </summary>
    Cone,

    /// <summary>
    /// Targets adjacent tile, affecting a 3-tile arc (cleave).
    /// </summary>
    Cleave
}
