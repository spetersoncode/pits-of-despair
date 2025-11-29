using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized utility for converting creature threat values to qualitative display text and colors.
/// Threat is relative to player level for intuitive comparison.
/// </summary>
public static class ThreatStatus
{
    /// <summary>
    /// Qualitative threat tier for display purposes.
    /// </summary>
    public enum Tier
    {
        Trivial,
        Easy,
        Moderate,
        Dangerous,
        Deadly
    }

    /// <summary>
    /// Gets both text and color for threat display relative to player level.
    /// </summary>
    /// <param name="creatureThreat">The creature's threat rating.</param>
    /// <param name="playerLevel">The player's current level.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetThreatDisplay(int creatureThreat, int playerLevel)
    {
        var tier = GetThreatTier(creatureThreat, playerLevel);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts creature threat relative to player level to a display tier.
    /// </summary>
    public static Tier GetThreatTier(int creatureThreat, int playerLevel)
    {
        int difference = creatureThreat - playerLevel;

        return difference switch
        {
            <= -2 => Tier.Trivial,
            -1 => Tier.Easy,
            0 => Tier.Moderate,
            1 => Tier.Dangerous,
            _ => Tier.Deadly
        };
    }

    /// <summary>
    /// Gets display text for a threat tier.
    /// </summary>
    public static string GetTierText(Tier tier) => tier switch
    {
        Tier.Trivial => "Trivial",
        Tier.Easy => "Easy",
        Tier.Moderate => "Moderate",
        Tier.Dangerous => "Dangerous",
        Tier.Deadly => "Deadly",
        _ => "Moderate"
    };

    /// <summary>
    /// Gets display color for a threat tier.
    /// </summary>
    public static Color GetTierColor(Tier tier) => tier switch
    {
        Tier.Trivial => Palette.SpeedVeryFast,   // Cyan - very safe
        Tier.Easy => Palette.SpeedFast,          // Green - safe
        Tier.Moderate => Palette.SpeedAverage,   // Gray - neutral
        Tier.Dangerous => Palette.SpeedSlow,     // Orange - caution
        Tier.Deadly => Palette.HealthCritical,   // Red - danger
        _ => Palette.SpeedAverage
    };
}
