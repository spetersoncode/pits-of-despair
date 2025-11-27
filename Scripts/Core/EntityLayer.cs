namespace PitsOfDespair.Core;

/// <summary>
/// Defines the rendering layer for entities.
/// Higher values render on top of lower values when multiple entities occupy the same tile.
/// </summary>
public enum EntityLayer
{
    /// <summary>
    /// Atmospheric decorations (vases, rubble, braziers).
    /// Lowest priority - rendered first, underneath everything.
    /// </summary>
    Decoration = 0,

    /// <summary>
    /// Interactive map features (stairs, gold, throne).
    /// Rendered above decorations, below items.
    /// </summary>
    Feature = 1,

    /// <summary>
    /// Collectible items (weapons, potions, scrolls).
    /// Rendered above features, below creatures.
    /// </summary>
    Item = 2,

    /// <summary>
    /// AI-controlled entities (monsters, companions, NPCs).
    /// Rendered above items, below player.
    /// </summary>
    Creature = 3,

    /// <summary>
    /// The player character.
    /// Highest priority - always rendered on top.
    /// </summary>
    Player = 4
}
