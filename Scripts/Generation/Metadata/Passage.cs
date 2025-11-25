using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Represents a narrow area connecting two or more regions.
/// Passages are identified by width <= MaxPassageWidth (default 2).
/// </summary>
public class Passage
{
    /// <summary>
    /// Unique identifier for this passage.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// First connected region ID.
    /// </summary>
    public int RegionAId { get; init; }

    /// <summary>
    /// Second connected region ID.
    /// </summary>
    public int RegionBId { get; init; }

    /// <summary>
    /// All tiles comprising this passage.
    /// </summary>
    public List<GridPosition> Tiles { get; init; } = new();

    /// <summary>
    /// Minimum width of this passage (1 = chokepoint).
    /// </summary>
    public int MinWidth { get; init; }

    /// <summary>
    /// Length of the passage in tiles.
    /// </summary>
    public int Length => Tiles.Count;
}
