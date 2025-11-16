using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Systems.Spawning.Placement;

/// <summary>
/// Selects positions near the center of the room.
/// Places the first entity at the center, then radiates outward.
/// </summary>
public class CenterPlacement : IPlacementStrategy
{
    public string Name => "center";

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

        // Filter out occupied positions
        var unoccupiedTiles = availableTiles
            .Where(tile => !occupiedPositions.Contains(tile))
            .ToList();

        if (unoccupiedTiles.Count == 0)
        {
            return new List<Vector2I>();
        }

        // Calculate center point of available tiles
        Vector2 centerPoint = CalculateCenter(unoccupiedTiles);

        // Sort tiles by distance from center
        var sortedByDistance = unoccupiedTiles
            .OrderBy(tile =>
            {
                float dx = tile.X - centerPoint.X;
                float dy = tile.Y - centerPoint.Y;
                return dx * dx + dy * dy;
            })
            .ToList();

        // Select the closest tiles up to count
        return sortedByDistance.Take(count).ToList();
    }

    private Vector2 CalculateCenter(List<Vector2I> tiles)
    {
        if (tiles.Count == 0)
        {
            return Vector2.Zero;
        }

        float sumX = 0;
        float sumY = 0;

        foreach (var tile in tiles)
        {
            sumX += tile.X;
            sumY += tile.Y;
        }

        return new Vector2(sumX / tiles.Count, sumY / tiles.Count);
    }
}
