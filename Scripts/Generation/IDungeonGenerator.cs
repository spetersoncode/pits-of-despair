using PitsOfDespair.Core;

namespace PitsOfDespair.Generation;

/// <summary>
/// Interface for dungeon generation algorithms.
/// Implementations provide different procedural generation strategies.
/// Each implementation manages its own configuration internally.
/// </summary>
public interface IDungeonGenerator
{
    /// <summary>
    /// Generates a dungeon map with the specified dimensions.
    /// </summary>
    /// <param name="width">Width of the map in tiles.</param>
    /// <param name="height">Height of the map in tiles.</param>
    /// <returns>A 2D array of TileType representing the generated dungeon.</returns>
    TileType[,] Generate(int width, int height);
}
