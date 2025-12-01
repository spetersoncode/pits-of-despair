using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized utility for converting health percentages to injury state descriptors.
/// Extracted from GameHUD and EntityDetailModal to eliminate duplication.
/// </summary>
public static class InjuryState
{
    /// <summary>
    /// Qualitative injury state tier for display purposes.
    /// </summary>
    public enum Tier
    {
        Uninjured,
        SlightlyWounded,
        Wounded,
        SeverelyWounded,
        NearDeath
    }

    /// <summary>
    /// Gets both text and color for injury state display.
    /// </summary>
    /// <param name="currentHP">The entity's current HP.</param>
    /// <param name="maxHP">The entity's maximum HP.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetInjuryDisplay(int currentHP, int maxHP)
    {
        var tier = GetInjuryTier(currentHP, maxHP);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts current HP percentage to an injury tier.
    /// Uses existing thresholds from the codebase: 100%, 75%+, 50%+, 25%+, &lt;25%
    /// </summary>
    public static Tier GetInjuryTier(int currentHP, int maxHP)
    {
        if (maxHP <= 0) return Tier.Uninjured;

        float hpPercent = (float)currentHP / maxHP;

        return hpPercent switch
        {
            >= 1.0f => Tier.Uninjured,
            >= 0.75f => Tier.SlightlyWounded,
            >= 0.50f => Tier.Wounded,
            >= 0.25f => Tier.SeverelyWounded,
            _ => Tier.NearDeath
        };
    }

    /// <summary>
    /// Gets display text for an injury tier (title case for UI display).
    /// </summary>
    public static string GetTierText(Tier tier) => tier switch
    {
        Tier.Uninjured => "Uninjured",
        Tier.SlightlyWounded => "Slightly Wounded",
        Tier.Wounded => "Wounded",
        Tier.SeverelyWounded => "Severely Wounded",
        Tier.NearDeath => "Near Death",
        _ => "Uninjured"
    };

    /// <summary>
    /// Gets display text for an injury tier (lowercase for message integration).
    /// Returns null for Uninjured since we typically don't mention full health.
    /// </summary>
    public static string? GetTierTextLower(Tier tier) => tier switch
    {
        Tier.Uninjured => null,
        Tier.SlightlyWounded => "slightly wounded",
        Tier.Wounded => "wounded",
        Tier.SeverelyWounded => "severely wounded",
        Tier.NearDeath => "near death",
        _ => null
    };

    /// <summary>
    /// Gets display color for an injury tier.
    /// </summary>
    public static Color GetTierColor(Tier tier) => tier switch
    {
        Tier.Uninjured => Palette.Success,
        Tier.SlightlyWounded => Palette.Success,
        Tier.Wounded => Palette.Caution,
        Tier.SeverelyWounded => Palette.Alert,
        Tier.NearDeath => Palette.Crimson,
        _ => Palette.Success
    };
}
