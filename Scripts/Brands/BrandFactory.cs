using Godot;
using PitsOfDespair.Data;

namespace PitsOfDespair.Brands;

/// <summary>
/// Single source of truth for creating Brand instances from type strings.
/// Used by effects, item generation, and any other system that needs to apply brands.
/// </summary>
public static class BrandFactory
{
    /// <summary>
    /// Creates a brand instance from a type string.
    /// </summary>
    /// <param name="brandType">The brand type (e.g., "damage", "accuracy", "flaming").</param>
    /// <param name="amount">The amount/magnitude of the brand (if applicable).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3", "permanent").</param>
    /// <param name="sourceId">Optional source identifier for tracking brand origin.</param>
    /// <returns>A new Brand instance, or null if the type is unknown.</returns>
    public static Brand? Create(
        string? brandType,
        int amount = 0,
        string duration = "permanent",
        string? sourceId = null)
    {
        if (string.IsNullOrEmpty(brandType))
        {
            GD.PrintErr("BrandFactory: brandType is null or empty");
            return null;
        }

        Brand? brand = brandType.ToLower() switch
        {
            // Weapon bonus brands
            "damage" => new DamageBrand(amount, duration, sourceId),
            "accuracy" => new AccuracyBrand(amount, duration, sourceId),

            // Elemental brands (all use ElementalBrand with different damage types)
            "flaming" => new ElementalBrand(DamageType.Fire, amount, duration, sourceId),
            "freezing" => new ElementalBrand(DamageType.Cold, amount, duration, sourceId),
            "electrified" => new ElementalBrand(DamageType.Lightning, amount, duration, sourceId),
            "venomous" => new ElementalBrand(DamageType.Poison, amount, duration, sourceId),

            // On-hit brands
            "vampiric" => new VampiricBrand(amount, duration, sourceId),

            _ => null
        };

        if (brand == null)
        {
            GD.PrintErr($"BrandFactory: Unknown brand type '{brandType}'");
        }

        return brand;
    }

    /// <summary>
    /// Checks if a brand type string is valid and recognized.
    /// </summary>
    public static bool IsValidType(string? brandType)
    {
        if (string.IsNullOrEmpty(brandType))
            return false;

        return brandType.ToLower() switch
        {
            "damage" => true,
            "accuracy" => true,
            "flaming" => true,
            "freezing" => true,
            "electrified" => true,
            "venomous" => true,
            "vampiric" => true,
            _ => false
        };
    }
}
