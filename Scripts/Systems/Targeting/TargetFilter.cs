namespace PitsOfDespair.Targeting;

/// <summary>
/// Defines what types of targets are valid for targeting.
/// Used to filter valid positions and creature cycling.
/// </summary>
public enum TargetFilter
{
    /// <summary>
    /// Only the caster/user is a valid target.
    /// </summary>
    Self,

    /// <summary>
    /// Only entities of a different faction are valid.
    /// </summary>
    Enemy,

    /// <summary>
    /// Only entities of the same faction are valid.
    /// </summary>
    Ally,

    /// <summary>
    /// Any entity (enemy or ally) is valid.
    /// </summary>
    Creature,

    /// <summary>
    /// Any tile is valid, regardless of whether it has an entity.
    /// </summary>
    Tile
}
