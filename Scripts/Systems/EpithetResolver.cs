using System.Linq;
using PitsOfDespair.Data;

namespace PitsOfDespair.Systems;

/// <summary>
/// Resolves the most appropriate epithet based on player base stats.
/// Selection algorithm: Filter matching epithets, sort by Priority descending,
/// then by TotalRequirement descending as a tiebreaker.
/// </summary>
public static class EpithetResolver
{
    private const string DefaultEpithet = "Wanderer";

    /// <summary>
    /// Resolves the best matching epithet for the given base stats.
    /// </summary>
    /// <param name="str">Base Strength</param>
    /// <param name="agi">Base Agility</param>
    /// <param name="end">Base Endurance</param>
    /// <param name="wil">Base Will</param>
    /// <returns>The name of the best matching epithet</returns>
    public static string Resolve(int str, int agi, int end, int wil)
    {
        var match = EpithetRegistry.Epithets
            .Where(e => e.Matches(str, agi, end, wil))
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.TotalRequirement)
            .FirstOrDefault();

        return match?.Name ?? DefaultEpithet;
    }
}
