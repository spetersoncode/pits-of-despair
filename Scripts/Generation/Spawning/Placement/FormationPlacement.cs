using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Generation.Spawning.Placement;

/// <summary>
/// Formation types for positioning entities.
/// </summary>
public enum FormationType
{
    Line,      // Horizontal line
    Circle,    // Circle around anchor
    Square,    // Square formation
    Scattered  // Loosely scattered
}

/// <summary>
/// Places entities in specific formations (line, circle, square).
/// Useful for creating structured encounters.
/// </summary>
public class FormationPlacement : IPlacementStrategy
{
    public string Name => "formation";

    private readonly FormationType _formation;

    public FormationPlacement(FormationType formation = FormationType.Line)
    {
        _formation = formation;
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

        // Filter out occupied positions
        var unoccupiedTiles = availableTiles
            .Where(tile => !occupiedPositions.Contains(tile))
            .ToList();

        if (unoccupiedTiles.Count == 0)
        {
            return new List<Vector2I>();
        }

        // If no anchor, use center of room
        Vector2I anchor = anchorPosition ?? CalculateCenterTile(unoccupiedTiles);

        return _formation switch
        {
            FormationType.Line => SelectLineFormation(unoccupiedTiles, count, anchor),
            FormationType.Circle => SelectCircleFormation(unoccupiedTiles, count, anchor),
            FormationType.Square => SelectSquareFormation(unoccupiedTiles, count, anchor),
            FormationType.Scattered => SelectScatteredFormation(unoccupiedTiles, count, anchor),
            _ => new RandomPlacement().SelectPositions(availableTiles, count, occupiedPositions, anchorPosition)
        };
    }

    private List<Vector2I> SelectLineFormation(List<Vector2I> tiles, int count, Vector2I anchor)
    {
        // Find tiles in a horizontal line near the anchor
        var lineTiles = tiles
            .Where(tile => Mathf.Abs(tile.Y - anchor.Y) <= 1)
            .OrderBy(tile => Mathf.Abs(tile.X - anchor.X))
            .Take(count)
            .ToList();

        return lineTiles.Count >= count ? lineTiles : SelectFallback(tiles, count);
    }

    private List<Vector2I> SelectCircleFormation(List<Vector2I> tiles, int count, Vector2I anchor)
    {
        // Select tiles at similar distances from anchor
        int targetRadius = 2;
        var circleTiles = tiles
            .OrderBy(tile => Mathf.Abs(CalculateChebyshevDistance(tile, anchor) - targetRadius))
            .Take(count)
            .ToList();

        return circleTiles;
    }

    private List<Vector2I> SelectSquareFormation(List<Vector2I> tiles, int count, Vector2I anchor)
    {
        // Select tiles forming a rough square around anchor
        var squareTiles = tiles
            .Where(tile =>
            {
                int dx = Mathf.Abs(tile.X - anchor.X);
                int dy = Mathf.Abs(tile.Y - anchor.Y);
                return (dx >= 1 && dx <= 2) || (dy >= 1 && dy <= 2);
            })
            .Take(count)
            .ToList();

        return squareTiles.Count >= count ? squareTiles : SelectFallback(tiles, count);
    }

    private List<Vector2I> SelectScatteredFormation(List<Vector2I> tiles, int count, Vector2I anchor)
    {
        // Select tiles spread out from each other
        var selectedPositions = new List<Vector2I>();
        var remainingTiles = new List<Vector2I>(tiles);

        // Start near anchor
        var firstTile = remainingTiles.OrderBy(t => CalculateChebyshevDistance(t, anchor)).FirstOrDefault();
        if (firstTile != default)
        {
            selectedPositions.Add(firstTile);
            remainingTiles.Remove(firstTile);
        }

        // Select subsequent tiles that are far from already selected tiles
        while (selectedPositions.Count < count && remainingTiles.Count > 0)
        {
            var nextTile = remainingTiles
                .OrderByDescending(tile => selectedPositions.Min(selected => CalculateChebyshevDistance(tile, selected)))
                .FirstOrDefault();

            if (nextTile != default)
            {
                selectedPositions.Add(nextTile);
                remainingTiles.Remove(nextTile);
            }
            else
            {
                break;
            }
        }

        return selectedPositions;
    }

    private List<Vector2I> SelectFallback(List<Vector2I> tiles, int count)
    {
        return tiles.Take(count).ToList();
    }

    private Vector2I CalculateCenterTile(List<Vector2I> tiles)
    {
        if (tiles.Count == 0)
        {
            return Vector2I.Zero;
        }

        int sumX = 0;
        int sumY = 0;

        foreach (var tile in tiles)
        {
            sumX += tile.X;
            sumY += tile.Y;
        }

        return new Vector2I(sumX / tiles.Count, sumY / tiles.Count);
    }

    private int CalculateChebyshevDistance(Vector2I a, Vector2I b)
    {
        int dx = Mathf.Abs(a.X - b.X);
        int dy = Mathf.Abs(a.Y - b.Y);
        return Mathf.Max(dx, dy);
    }
}
