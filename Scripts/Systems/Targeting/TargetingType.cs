namespace PitsOfDespair.Targeting;

/// <summary>
/// Defines how a targeting source (skill, item, weapon) selects its targets.
/// </summary>
public enum TargetingType
{
    /// <summary>
    /// Targets the caster/user. No selection needed.
    /// </summary>
    Self,

    /// <summary>
    /// Targets one of the 8 adjacent tiles.
    /// </summary>
    Adjacent,

    /// <summary>
    /// Targets any tile within range. May or may not have an entity.
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
    /// Targets any creature (enemy or ally) within range.
    /// </summary>
    Creature,

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
    Cone,

    /// <summary>
    /// Ranged attack targeting - uses Euclidean distance with LOS.
    /// </summary>
    Ranged,

    /// <summary>
    /// Reach attack targeting - uses Chebyshev distance with LOS.
    /// Suitable for polearms, spears, and other extended melee weapons.
    /// </summary>
    Reach,

    /// <summary>
    /// Cleave attack - targets adjacent tile and affects a 3-tile arc.
    /// </summary>
    Cleave
}
