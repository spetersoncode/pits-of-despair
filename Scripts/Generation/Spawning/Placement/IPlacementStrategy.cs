using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Generation.Spawning.Placement;

/// <summary>
/// Interface for placement strategies that determine where entities spawn in rooms.
/// </summary>
public interface IPlacementStrategy
{
    /// <summary>
    /// Gets the name of this placement strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Selects positions for spawning entities in a room.
    /// </summary>
    /// <param name="availableTiles">List of all available floor tiles in the room</param>
    /// <param name="count">Number of positions to select</param>
    /// <param name="occupiedPositions">Set of already occupied positions to avoid</param>
    /// <param name="anchorPosition">Optional anchor position (e.g., leader position for followers)</param>
    /// <returns>List of selected positions, may be fewer than requested if positions unavailable</returns>
    List<Vector2I> SelectPositions(
        List<Vector2I> availableTiles,
        int count,
        HashSet<Vector2I> occupiedPositions,
        Vector2I? anchorPosition = null
    );
}
