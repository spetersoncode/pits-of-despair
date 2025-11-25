using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.Spawning.Placement;

/// <summary>
/// Places entities based on distance from a reference point using metadata distance fields.
/// Useful for placing enemies far from entrance or treasures near walls.
/// </summary>
public class DistanceBasedPlacement : IPlacementStrategy
{
    private readonly int _minDistance;
    private readonly int _maxDistance;
    private readonly string _distanceFieldName;

    public string Name { get; }

    /// <summary>
    /// Create a distance-based placement strategy.
    /// </summary>
    /// <param name="minDistance">Minimum distance from reference point.</param>
    /// <param name="maxDistance">Maximum distance from reference point (int.MaxValue for no limit).</param>
    /// <param name="fieldName">Distance field to use: "entrance", "exit", or "walls".</param>
    /// <param name="name">Optional custom name for this strategy instance.</param>
    public DistanceBasedPlacement(int minDistance, int maxDistance, string fieldName = "entrance", string name = null)
    {
        _minDistance = minDistance;
        _maxDistance = maxDistance;
        _distanceFieldName = fieldName;
        Name = name ?? $"distance_{fieldName}_{minDistance}_{maxDistance}";
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

        // Check if metadata is available
        var metadata = MetadataProvider.Current;
        if (metadata == null)
        {
            // Fallback to random placement if no metadata
            GD.Print($"[DistanceBasedPlacement] No metadata, falling back to random");
            return new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions, anchorPosition);
        }

        // Get the appropriate distance field
        var distanceField = _distanceFieldName switch
        {
            "entrance" => metadata.EntranceDistance,
            "exit" => metadata.ExitDistance,
            "walls" => metadata.WallDistance,
            _ => metadata.EntranceDistance
        };

        if (distanceField == null)
        {
            GD.Print($"[DistanceBasedPlacement] Distance field '{_distanceFieldName}' not available, falling back to random");
            return new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions, anchorPosition);
        }

        // Filter tiles by distance range
        var validTiles = availableTiles
            .Where(t => !occupiedPositions.Contains(t))
            .Where(t =>
            {
                int dist = distanceField.GetDistance(new GridPosition(t.X, t.Y));
                return dist >= _minDistance && dist <= _maxDistance;
            })
            .ToList();

        if (validTiles.Count == 0)
        {
            GD.Print($"[DistanceBasedPlacement] No tiles in distance range [{_minDistance}, {_maxDistance}], falling back to random");
            return new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions, anchorPosition);
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
