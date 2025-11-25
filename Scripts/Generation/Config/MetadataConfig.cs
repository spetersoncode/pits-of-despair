using System.Collections.Generic;

namespace PitsOfDespair.Generation.Config;

/// <summary>
/// Configuration for metadata analysis pass.
/// </summary>
public class MetadataConfig
{
    /// <summary>
    /// Minimum tiles for an area to be classified as a Region (vs Alcove).
    /// Default: 16
    /// </summary>
    public int MinRegionSize { get; set; } = 16;

    /// <summary>
    /// Maximum width for an area to be classified as a Passage.
    /// Default: 2
    /// </summary>
    public int MaxPassageWidth { get; set; } = 2;

    /// <summary>
    /// Distance fields to compute during analysis.
    /// Options: "walls", "entrance", "exit"
    /// </summary>
    public List<string> DistanceFields { get; set; } = new() { "walls", "entrance", "exit" };
}
