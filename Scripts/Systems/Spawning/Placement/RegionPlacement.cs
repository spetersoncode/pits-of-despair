using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Systems.Spawning.Placement;

/// <summary>
/// Places entities within a specific region's bounds.
/// Used for prefab spawn hints and region-targeted spawning.
/// </summary>
public class RegionPlacement : IPlacementStrategy
{
    private readonly int? _regionId;
    private readonly Region _region;

    public string Name => _regionId.HasValue ? $"region_{_regionId}" : "region";

    /// <summary>
    /// Create a region-constrained placement strategy by region ID.
    /// Looks up the region from current metadata.
    /// </summary>
    /// <param name="regionId">The region ID to constrain placement to.</param>
    public RegionPlacement(int regionId)
    {
        _regionId = regionId;
        _region = null;
    }

    /// <summary>
    /// Create a region-constrained placement strategy with a specific region.
    /// </summary>
    /// <param name="region">The region to constrain placement to.</param>
    public RegionPlacement(Region region)
    {
        _regionId = null;
        _region = region;
    }

    public List<Vector2I> SelectPositions(
        List<Vector2I> availableTiles,
        int count,
        HashSet<Vector2I> occupiedPositions,
        Vector2I? anchorPosition = null)
    {
        if (availableTiles == null || availableTiles.Count == 0)
        {
            return new List<Vector2I>();
        }

        // Get the target region
        Region targetRegion = _region;
        if (targetRegion == null && _regionId.HasValue)
        {
            var metadata = MetadataProvider.Current;
            if (metadata != null && _regionId.Value >= 0 && _regionId.Value < metadata.Regions.Count)
            {
                targetRegion = metadata.Regions[_regionId.Value];
            }
        }

        if (targetRegion == null)
        {
            // Fallback to random if region not found
            GD.Print($"[RegionPlacement] Region not found, falling back to random");
            return new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions, anchorPosition);
        }

        // Build set of region tiles for fast lookup
        var regionTileSet = new HashSet<Vector2I>(
            targetRegion.Tiles.Select(t => new Vector2I(t.X, t.Y)));

        // Filter available tiles to those in the region
        var validTiles = availableTiles
            .Where(t => regionTileSet.Contains(t))
            .Where(t => !occupiedPositions.Contains(t))
            .ToList();

        if (validTiles.Count == 0)
        {
            GD.Print($"[RegionPlacement] No available tiles in region {targetRegion.Id}");
            return new List<Vector2I>();
        }

        // Randomly select from valid tiles
        var selectedPositions = new List<Vector2I>();
        var availableForSelection = new List<Vector2I>(validTiles);

        for (int i = 0; i < count && availableForSelection.Count > 0; i++)
        {
            int randomIndex = GD.RandRange(0, availableForSelection.Count - 1);
            selectedPositions.Add(availableForSelection[randomIndex]);
            availableForSelection.RemoveAt(randomIndex);
        }

        return selectedPositions;
    }
}
