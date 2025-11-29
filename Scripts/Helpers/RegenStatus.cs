using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized utility for converting regeneration rate values to qualitative display text and colors.
/// Uses the same tier system as SpeedStatus for consistency.
/// </summary>
public static class RegenStatus
{
    /// <summary>
    /// Qualitative regen tier for display purposes.
    /// Matches SpeedStatus.Tier for visual consistency.
    /// </summary>
    public enum Tier
    {
        VeryFast,
        Fast,
        Average,
        Slow,
        VerySlow
    }

    /// <summary>
    /// Gets both text and color for regeneration display.
    /// BaseRegenRate formula is: 20 + MaxHealth / 6 + TotalRegenBonus
    /// A new player starts with 23 base regen (20 + 20/6).
    /// Ring of Regeneration adds +80, bringing total to ~100+.
    /// </summary>
    /// <param name="baseRegenRate">The entity's base regeneration rate.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetRegenDisplay(int baseRegenRate)
    {
        var tier = GetRegenTier(baseRegenRate);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts base regen rate to a display tier.
    /// Centered on Average for typical player regen (~23 at start).
    /// </summary>
    public static Tier GetRegenTier(int baseRegenRate)
    {
        return baseRegenRate switch
        {
            <= 15 => Tier.VerySlow,
            <= 20 => Tier.Slow,
            <= 35 => Tier.Average,
            <= 60 => Tier.Fast,
            _ => Tier.VeryFast
        };
    }

    /// <summary>
    /// Gets display text for a regen tier.
    /// </summary>
    public static string GetTierText(Tier tier) => tier switch
    {
        Tier.VeryFast => "Very Fast",
        Tier.Fast => "Fast",
        Tier.Average => "Average",
        Tier.Slow => "Slow",
        Tier.VerySlow => "Very Slow",
        _ => "Average"
    };

    /// <summary>
    /// Gets display color for a regen tier.
    /// Uses same colors as SpeedStatus for consistency.
    /// </summary>
    public static Color GetTierColor(Tier tier) => tier switch
    {
        Tier.VeryFast => Palette.SpeedVeryFast,
        Tier.Fast => Palette.SpeedFast,
        Tier.Average => Palette.SpeedAverage,
        Tier.Slow => Palette.SpeedSlow,
        Tier.VerySlow => Palette.SpeedVerySlow,
        _ => Palette.SpeedAverage
    };
}
