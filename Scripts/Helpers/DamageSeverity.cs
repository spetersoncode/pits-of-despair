using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized utility for converting damage values to qualitative severity descriptors.
/// Severity is calculated as a percentage of target's maximum HP.
/// </summary>
public static class DamageSeverity
{
    /// <summary>
    /// Qualitative damage severity tier for display purposes.
    /// </summary>
    public enum Tier
    {
        Minor,
        Moderate,
        Heavy,
        Devastating
    }

    /// <summary>
    /// Gets both text and color for damage severity display.
    /// </summary>
    /// <param name="damage">The damage dealt.</param>
    /// <param name="targetMaxHP">The target's maximum HP.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetSeverityDisplay(int damage, int targetMaxHP)
    {
        var tier = GetSeverityTier(damage, targetMaxHP);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts damage relative to target max HP to a severity tier.
    /// Thresholds: &lt;15% minor, 15-35% moderate, 35-60% heavy, 60%+ devastating
    /// </summary>
    public static Tier GetSeverityTier(int damage, int targetMaxHP)
    {
        if (targetMaxHP <= 0) return Tier.Minor;

        float percent = (float)damage / targetMaxHP * 100f;

        return percent switch
        {
            < 15f => Tier.Minor,
            < 35f => Tier.Moderate,
            < 60f => Tier.Heavy,
            _ => Tier.Devastating
        };
    }

    /// <summary>
    /// Gets display text for a severity tier (lowercase for message integration).
    /// </summary>
    public static string GetTierText(Tier tier) => tier switch
    {
        Tier.Minor => "minor",
        Tier.Moderate => "moderate",
        Tier.Heavy => "heavy",
        Tier.Devastating => "devastating",
        _ => "minor"
    };

    /// <summary>
    /// Gets display color for a severity tier.
    /// </summary>
    public static Color GetTierColor(Tier tier) => tier switch
    {
        Tier.Minor => Palette.Default,
        Tier.Moderate => Palette.Caution,
        Tier.Heavy => Palette.Alert,
        Tier.Devastating => Palette.Danger,
        _ => Palette.Default
    };
}
