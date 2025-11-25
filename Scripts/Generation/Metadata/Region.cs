using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Represents a contiguous walkable area in the dungeon.
/// Minimum size defined by MinRegionSize in metadata config (default 16 tiles).
/// </summary>
public class Region
{
    /// <summary>
    /// Unique identifier for this region.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// All walkable tiles in this region.
    /// </summary>
    public List<GridPosition> Tiles { get; init; } = new();

    /// <summary>
    /// Tiles adjacent to walls (subset of Tiles).
    /// </summary>
    public List<GridPosition> EdgeTiles { get; init; } = new();

    /// <summary>
    /// Axis-aligned bounding box containing all tiles.
    /// </summary>
    public Rect2I BoundingBox { get; init; }

    /// <summary>
    /// Geometric center of the region.
    /// </summary>
    public GridPosition Centroid { get; init; }

    /// <summary>
    /// Number of tiles in this region.
    /// </summary>
    public int Area => Tiles.Count;

    /// <summary>
    /// How this region was created.
    /// </summary>
    public RegionSource Source { get; set; } = RegionSource.Detected;

    /// <summary>
    /// Optional semantic tag (e.g., "treasure_room", "entrance").
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Spawn hints from prefabs or generation passes.
    /// </summary>
    public List<SpawnHint> SpawnHints { get; set; } = new();

    /// <summary>
    /// Custom data from generation passes.
    /// </summary>
    public Dictionary<string, object> CustomData { get; } = new();
}
