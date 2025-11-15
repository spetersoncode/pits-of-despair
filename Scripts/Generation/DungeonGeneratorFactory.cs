using System;
using PitsOfDespair.Generation.Generators;

namespace PitsOfDespair.Generation;

/// <summary>
/// Factory for creating dungeon generator instances.
/// Supports adding new generators without modifying existing code.
/// </summary>
public static class DungeonGeneratorFactory
{
    /// <summary>
    /// Creates a BSP dungeon generator instance with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration for the BSP generator.</param>
    /// <returns>An instance of IDungeonGenerator.</returns>
    public static IDungeonGenerator CreateBSP(BSPConfig config)
    {
        return new BSPDungeonGenerator(config);
    }

    // Future generator factory methods will go here:
    // public static IDungeonGenerator CreateCellularAutomata(CellularAutomataConfig config) { ... }
    // public static IDungeonGenerator CreateDrunkardWalk(DrunkardWalkConfig config) { ... }
}
