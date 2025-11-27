using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Generation.Spawning.Placement;

/// <summary>
/// Randomly selects positions from available tiles, avoiding occupied positions.
/// </summary>
public class RandomPlacement : IPlacementStrategy
{
    public string Name => "random";

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

        // Randomly select up to 'count' positions
        var selectedPositions = new List<Vector2I>();
        var availableForSelection = new List<Vector2I>(unoccupiedTiles);

        for (int i = 0; i < count && availableForSelection.Count > 0; i++)
        {
            int randomIndex = GD.RandRange(0, availableForSelection.Count - 1);
            selectedPositions.Add(availableForSelection[randomIndex]);
            availableForSelection.RemoveAt(randomIndex);
        }

        return selectedPositions;
    }
}
