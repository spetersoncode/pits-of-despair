using System;

namespace PitsOfDespair.Generation.Spawning;

/// <summary>
/// Calculates gold amounts based on floor depth using exponential growth.
/// Keeps pile count relatively stable while pile size scales with depth.
/// </summary>
public static class GoldFormula
{
    private const int BaseFloorGold = 50;      // Total gold budget for floor 1
    private const float GrowthRate = 1.2f;     // 20% more gold per floor
    private const int MinPileCount = 7;        // Minimum piles per floor
    private const int MaxPileCount = 12;       // Maximum piles per floor

    /// <summary>
    /// Calculate total gold budget for a floor.
    /// Uses exponential growth: base × 1.2^(depth-1)
    /// </summary>
    public static int GetTotalGoldBudget(int floorDepth)
    {
        return (int)(BaseFloorGold * Math.Pow(GrowthRate, floorDepth - 1));
    }

    /// <summary>
    /// Get pile count range (slight variance, doesn't scale with depth).
    /// </summary>
    public static (int min, int max) GetPileCountRange()
    {
        return (MinPileCount, MaxPileCount);
    }

    /// <summary>
    /// Get pile size range - grows with floor depth.
    /// </summary>
    /// <param name="floorDepth">Current floor depth</param>
    /// <param name="actualPileCount">Actual number of piles being placed</param>
    /// <returns>Min and max gold per pile</returns>
    public static (int min, int max) GetPileSizeRange(int floorDepth, int actualPileCount)
    {
        int budget = GetTotalGoldBudget(floorDepth);
        int avgPile = budget / Math.Max(1, actualPileCount);
        int variance = Math.Max(2, avgPile / 4);  // ~25% variance
        return (Math.Max(1, avgPile - variance), avgPile + variance);
    }
}
