namespace PitsOfDespair.Core;

/// <summary>
/// Defines the fundamental type of an entity.
/// Used for classification, queries, and deriving render layer.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// AI-controlled monsters, NPCs, companions.
    /// </summary>
    Creature,

    /// <summary>
    /// Collectible items (weapons, potions, scrolls).
    /// </summary>
    Item,

    /// <summary>
    /// Atmospheric objects (vases, rubble, braziers).
    /// </summary>
    Decoration,

    /// <summary>
    /// Interactive map features (stairs, throne).
    /// </summary>
    Feature,

    /// <summary>
    /// Currency pickup.
    /// </summary>
    Gold,

    /// <summary>
    /// The player character.
    /// </summary>
    Player
}
