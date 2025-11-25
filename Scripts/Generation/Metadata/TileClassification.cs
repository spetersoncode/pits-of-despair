namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Per-tile spatial category for gameplay purposes.
/// </summary>
public enum TileClassification
{
    /// <summary>
    /// Solid wall tile.
    /// </summary>
    Wall,

    /// <summary>
    /// Region interior, far from walls (wall distance > threshold).
    /// </summary>
    Open,

    /// <summary>
    /// Region tile adjacent to wall (wall distance = 1).
    /// </summary>
    Edge,

    /// <summary>
    /// Edge tile with walls on multiple sides.
    /// </summary>
    Corner,

    /// <summary>
    /// Narrow connecting area between regions.
    /// </summary>
    Passage,

    /// <summary>
    /// Minimal-width point in a passage (width = 1).
    /// </summary>
    Chokepoint,

    /// <summary>
    /// Passage with only one exit direction.
    /// </summary>
    DeadEnd,

    /// <summary>
    /// Small enclosed space (area < MinRegionSize).
    /// </summary>
    Alcove
}
