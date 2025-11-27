using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Generation.Spawning.Placement;

/// <summary>
/// Places entities around an anchor position (e.g., followers around a leader).
/// Prioritizes tiles within a specified distance range from the anchor.
/// </summary>
public class SurroundingPlacement : IPlacementStrategy
{
    public string Name => "surrounding";

    private readonly int _minDistance;
    private readonly int _maxDistance;

    public SurroundingPlacement(int minDistance = 1, int maxDistance = 2)
    {
        _minDistance = minDistance;
        _maxDistance = maxDistance;
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

        if (!anchorPosition.HasValue)
        {
            // Fallback to random if no anchor provided
            GD.PushWarning("SurroundingPlacement requires anchor position, falling back to random");
            return new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions);
        }

        // Filter out occupied positions
        var unoccupiedTiles = availableTiles
            .Where(tile => !occupiedPositions.Contains(tile))
            .ToList();

        if (unoccupiedTiles.Count == 0)
        {
            return new List<Vector2I>();
        }

        Vector2I anchor = anchorPosition.Value;

        // Get tiles within distance range, using Chebyshev distance (max of dx, dy)
        var tilesInRange = unoccupiedTiles
            .Where(tile =>
            {
                int dx = Mathf.Abs(tile.X - anchor.X);
                int dy = Mathf.Abs(tile.Y - anchor.Y);
                int distance = Mathf.Max(dx, dy);
                return distance >= _minDistance && distance <= _maxDistance;
            })
            .ToList();

        // If we don't have enough tiles in range, expand to all unoccupied tiles
        if (tilesInRange.Count < count)
        {
            tilesInRange = unoccupiedTiles
                .OrderBy(tile =>
                {
                    int dx = Mathf.Abs(tile.X - anchor.X);
                    int dy = Mathf.Abs(tile.Y - anchor.Y);
                    return Mathf.Max(dx, dy);
                })
                .ToList();
        }

        // Randomly select from tiles in range
        var selectedPositions = new List<Vector2I>();
        var availableForSelection = new List<Vector2I>(tilesInRange);

        for (int i = 0; i < count && availableForSelection.Count > 0; i++)
        {
            int randomIndex = GD.RandRange(0, availableForSelection.Count - 1);
            selectedPositions.Add(availableForSelection[randomIndex]);
            availableForSelection.RemoveAt(randomIndex);
        }

        return selectedPositions;
    }
}
