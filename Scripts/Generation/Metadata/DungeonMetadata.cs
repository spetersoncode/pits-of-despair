using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Container for all dungeon metadata computed during generation.
/// Provides spatial analysis data for downstream systems (spawning, AI, etc.)
///
/// Note: Full implementation in Phase 3-4. This is the minimal stub for Phase 1.
/// </summary>
public class DungeonMetadata
{
    /// <summary>
    /// List of detected regions (contiguous walkable areas >= MinRegionSize).
    /// </summary>
    public List<Region> Regions { get; } = new();

    /// <summary>
    /// Small areas that don't qualify as full regions (< MinRegionSize).
    /// </summary>
    public List<Region> Alcoves { get; } = new();

    /// <summary>
    /// Narrow areas connecting regions.
    /// </summary>
    public List<Passage> Passages { get; } = new();

    /// <summary>
    /// Tactical bottleneck positions.
    /// </summary>
    public List<Chokepoint> Chokepoints { get; } = new();

    /// <summary>
    /// Per-tile region assignment (-1 for passages/alcoves/walls).
    /// </summary>
    public int[,] RegionIds { get; set; }

    /// <summary>
    /// Per-tile spatial classification.
    /// </summary>
    public TileClassification[,] TileClassifications { get; set; }

    /// <summary>
    /// Distance from each tile to nearest wall.
    /// </summary>
    public DistanceField WallDistance { get; set; }

    /// <summary>
    /// Distance from each tile to entrance position.
    /// </summary>
    public DistanceField EntranceDistance { get; set; }

    /// <summary>
    /// Distance from each tile to exit position.
    /// </summary>
    public DistanceField ExitDistance { get; set; }

    /// <summary>
    /// Player starting position (set by spawning system).
    /// </summary>
    public GridPosition? EntrancePosition { get; set; }

    /// <summary>
    /// Stairs/exit position (set by spawning system).
    /// </summary>
    public GridPosition? ExitPosition { get; set; }

    /// <summary>
    /// Get the region containing the given position.
    /// </summary>
    public Region GetRegionAt(GridPosition pos)
    {
        if (RegionIds == null) return null;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= RegionIds.GetLength(0) || pos.Y >= RegionIds.GetLength(1))
            return null;

        int regionId = RegionIds[pos.X, pos.Y];
        if (regionId < 0 || regionId >= Regions.Count) return null;
        return Regions[regionId];
    }

    /// <summary>
    /// Get the tile classification at the given position.
    /// </summary>
    public TileClassification GetClassification(GridPosition pos)
    {
        if (TileClassifications == null) return TileClassification.Wall;
        if (pos.X < 0 || pos.Y < 0 || pos.X >= TileClassifications.GetLength(0) || pos.Y >= TileClassifications.GetLength(1))
            return TileClassification.Wall;

        return TileClassifications[pos.X, pos.Y];
    }

    /// <summary>
    /// Get regions that have spawn hints attached (from prefabs).
    /// </summary>
    public IReadOnlyList<Region> GetSpawnableRegions()
    {
        var result = new List<Region>();
        foreach (var region in Regions)
        {
            if (region.SpawnHints != null && region.SpawnHints.Count > 0)
                result.Add(region);
        }
        return result;
    }

    /// <summary>
    /// Get high-value strategic positions (chokepoints with high value).
    /// </summary>
    public IReadOnlyList<GridPosition> GetStrategicPositions()
    {
        var result = new List<GridPosition>();
        foreach (var choke in Chokepoints)
        {
            if (choke.StrategicValue > 0.5f)
                result.Add(choke.Position);
        }
        return result;
    }
}
