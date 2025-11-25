using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Represents a tactical bottleneck position in the dungeon.
/// Chokepoints are points of minimal width where traffic is funneled.
/// </summary>
public class Chokepoint
{
    /// <summary>
    /// Grid position of the chokepoint.
    /// </summary>
    public GridPosition Position { get; init; }

    /// <summary>
    /// Width of the passage at this point (1 = single-tile chokepoint).
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// IDs of regions connected through this chokepoint.
    /// </summary>
    public int[] ConnectedRegionIds { get; init; }

    /// <summary>
    /// Strategic value score (0-1). Higher = more traffic funneled through.
    /// Computed based on regions connected and alternative paths.
    /// </summary>
    public float StrategicValue { get; init; }

    /// <summary>
    /// ID of the passage containing this chokepoint (if any).
    /// </summary>
    public int? PassageId { get; init; }
}
