using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized utility for converting speed values to qualitative display text and colors.
/// Supports both creature movement speed and weapon attack delay conversion.
/// </summary>
public static class SpeedStatus
{
    /// <summary>
    /// Qualitative speed tier for display purposes.
    /// </summary>
    public enum Tier
    {
        VeryFast,
        Fast,
        Average,
        Slow,
        VerySlow
    }

    #region Creature Speed

    /// <summary>
    /// Gets both text and color for creature speed display.
    /// Speed 10 = Average. Higher = faster, lower = slower.
    /// </summary>
    /// <param name="effectiveSpeed">The creature's effective speed value.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetCreatureSpeedDisplay(int effectiveSpeed)
    {
        var tier = GetCreatureSpeedTier(effectiveSpeed);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts creature effective speed to a display tier.
    /// Based on effective delay for standard 10 aut action.
    /// </summary>
    public static Tier GetCreatureSpeedTier(int effectiveSpeed)
    {
        // Calculate effective delay: Standard (10) * Average (10) / speed
        float effectiveDelay = 100f / effectiveSpeed;

        return effectiveDelay switch
        {
            <= 7f => Tier.VeryFast,
            <= 9f => Tier.Fast,
            <= 11f => Tier.Average,
            <= 14f => Tier.Slow,
            _ => Tier.VerySlow
        };
    }

    #endregion

    #region Weapon Delay

    /// <summary>
    /// Gets both text and color for weapon speed display.
    /// Delay 1.0 = Average. Lower = faster, higher = slower.
    /// </summary>
    /// <param name="delayMultiplier">The weapon's delay multiplier.</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetWeaponSpeedDisplay(float delayMultiplier)
    {
        var tier = GetWeaponSpeedTier(delayMultiplier);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts weapon delay multiplier to a display tier.
    /// </summary>
    public static Tier GetWeaponSpeedTier(float delayMultiplier)
    {
        return delayMultiplier switch
        {
            <= 0.75f => Tier.VeryFast,
            <= 0.9f => Tier.Fast,
            <= 1.1f => Tier.Average,
            <= 1.25f => Tier.Slow,
            _ => Tier.VerySlow
        };
    }

    #endregion

    #region Combined Attack Speed

    /// <summary>
    /// Gets both text and color for combined attack speed (creature speed + weapon delay).
    /// This shows the actual attack tempo the player/creature experiences.
    /// </summary>
    /// <param name="effectiveSpeed">The creature's effective speed value.</param>
    /// <param name="weaponDelayCost">The weapon's delay cost in aut (from GetDelayCost()).</param>
    /// <returns>Tuple of display text and color.</returns>
    public static (string text, Color color) GetCombinedAttackSpeedDisplay(int effectiveSpeed, int weaponDelayCost)
    {
        // Calculate actual delay: weaponDelay * (10 / speed)
        float actualDelay = weaponDelayCost * (10f / effectiveSpeed);
        var tier = GetDelayTier(actualDelay);
        return (GetTierText(tier), GetTierColor(tier));
    }

    /// <summary>
    /// Converts actual delay in aut to a display tier.
    /// </summary>
    private static Tier GetDelayTier(float delayAut)
    {
        return delayAut switch
        {
            <= 7f => Tier.VeryFast,
            <= 9f => Tier.Fast,
            <= 11f => Tier.Average,
            <= 14f => Tier.Slow,
            _ => Tier.VerySlow
        };
    }

    #endregion

    #region Tier Utilities

    /// <summary>
    /// Gets display text for a speed tier.
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
    /// Gets display color for a speed tier.
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

    #endregion
}
