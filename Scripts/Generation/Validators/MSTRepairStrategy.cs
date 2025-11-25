using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Validators;

/// <summary>
/// Repairs disconnected dungeon areas using Minimum Spanning Tree approach.
/// Connects islands with minimal corridors using Prim's algorithm.
/// </summary>
public static class MSTRepairStrategy
{
    /// <summary>
    /// Repair disconnected islands by carving corridors between them.
    /// Uses MST to minimize total corridor length.
    /// </summary>
    public static int Repair(GenerationContext context, ConnectivityValidator.ValidationResult validation, int maxCorridorLength = 20)
    {
        if (validation.IsFullyConnected || validation.Islands.Count <= 1)
            return 0;

        var islands = validation.Islands;
        int corridorsCarved = 0;

        // Build MST using Prim's algorithm
        var connected = new HashSet<int> { validation.LargestIslandIndex };
        var remaining = new HashSet<int>();

        for (int i = 0; i < islands.Count; i++)
        {
            if (i != validation.LargestIslandIndex)
                remaining.Add(i);
        }

        while (remaining.Count > 0)
        {
            int bestFrom = -1;
            int bestTo = -1;
            int bestDist = int.MaxValue;
            GridPosition bestFromPos = default;
            GridPosition bestToPos = default;

            // Find closest pair between connected and remaining islands
            foreach (int fromIdx in connected)
            {
                foreach (int toIdx in remaining)
                {
                    var (from, to, dist) = ConnectivityValidator.FindClosestPair(
                        islands[fromIdx], islands[toIdx]);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = fromIdx;
                        bestTo = toIdx;
                        bestFromPos = from;
                        bestToPos = to;
                    }
                }
            }

            if (bestTo < 0)
                break;

            // Carve corridor if within max length
            if (bestDist <= maxCorridorLength)
            {
                CarveCorridor(context, bestFromPos, bestToPos);
                corridorsCarved++;
                GD.Print($"[MSTRepair] Connected island {bestTo} to {bestFrom} (distance: {bestDist})");
            }
            else
            {
                GD.PushWarning($"[MSTRepair] Island {bestTo} too far to connect (distance: {bestDist} > {maxCorridorLength})");
            }

            // Move island to connected set
            remaining.Remove(bestTo);
            connected.Add(bestTo);
        }

        return corridorsCarved;
    }

    /// <summary>
    /// Carve an L-shaped corridor between two points.
    /// </summary>
    private static void CarveCorridor(GenerationContext context, GridPosition from, GridPosition to)
    {
        // Decide whether to go horizontal-first or vertical-first
        bool horizontalFirst = context.Random.Next(2) == 0;

        if (horizontalFirst)
        {
            CarveHorizontal(context, from.X, to.X, from.Y);
            CarveVertical(context, from.Y, to.Y, to.X);
        }
        else
        {
            CarveVertical(context, from.Y, to.Y, from.X);
            CarveHorizontal(context, from.X, to.X, to.Y);
        }
    }

    private static void CarveHorizontal(GenerationContext context, int x1, int x2, int y)
    {
        int start = Math.Min(x1, x2);
        int end = Math.Max(x1, x2);

        for (int x = start; x <= end; x++)
        {
            // Don't carve through borders
            if (x > 0 && x < context.Width - 1 && y > 0 && y < context.Height - 1)
            {
                context.SetTile(x, y, TileType.Floor);
            }
        }
    }

    private static void CarveVertical(GenerationContext context, int y1, int y2, int x)
    {
        int start = Math.Min(y1, y2);
        int end = Math.Max(y1, y2);

        for (int y = start; y <= end; y++)
        {
            // Don't carve through borders
            if (x > 0 && x < context.Width - 1 && y > 0 && y < context.Height - 1)
            {
                context.SetTile(x, y, TileType.Floor);
            }
        }
    }
}
