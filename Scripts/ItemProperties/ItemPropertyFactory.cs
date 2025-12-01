using Godot;
using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Single source of truth for creating ItemProperty instances from type strings.
/// Used by effects, item generation, and any other system that needs to apply properties.
/// </summary>
public static class ItemPropertyFactory
{
    /// <summary>
    /// Creates a property instance from a type string.
    /// </summary>
    /// <param name="propertyType">The property type (e.g., "damage", "accuracy", "flaming").</param>
    /// <param name="amount">The amount/magnitude of the property (if applicable).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3", "permanent").</param>
    /// <param name="sourceId">Optional source identifier for tracking property origin.</param>
    /// <returns>A new ItemProperty instance, or null if the type is unknown.</returns>
    public static ItemProperty? Create(
        string? propertyType,
        int amount = 0,
        string duration = "permanent",
        string? sourceId = null)
    {
        if (string.IsNullOrEmpty(propertyType))
        {
            GD.PrintErr("ItemPropertyFactory: propertyType is null or empty");
            return null;
        }

        ItemProperty? property = propertyType.ToLower() switch
        {
            // Weapon bonus properties
            "damage" => new DamageProperty(amount, duration, sourceId),
            "accuracy" => new AccuracyProperty(amount, duration, sourceId),

            // Elemental properties (all use ElementalProperty with different damage types)
            "flaming" => new ElementalProperty(DamageType.Fire, amount, duration, sourceId),
            "freezing" => new ElementalProperty(DamageType.Cold, amount, duration, sourceId),
            "electrified" => new ElementalProperty(DamageType.Lightning, amount, duration, sourceId),
            "venomous" => new ElementalProperty(DamageType.Poison, amount, duration, sourceId),

            // On-hit properties
            "vampiric" => new VampiricProperty(amount, duration, sourceId),

            // Resistance properties (amount is percentage: 50 = half damage)
            "resistance_fire" => CreateResistanceProperty(DamageType.Fire, amount, duration, sourceId),
            "resistance_cold" => CreateResistanceProperty(DamageType.Cold, amount, duration, sourceId),
            "resistance_lightning" => CreateResistanceProperty(DamageType.Lightning, amount, duration, sourceId),
            "resistance_poison" => CreateResistanceProperty(DamageType.Poison, amount, duration, sourceId),
            "resistance_necrotic" => CreateResistanceProperty(DamageType.Necrotic, amount, duration, sourceId),
            "resistance_acid" => CreateResistanceProperty(DamageType.Acid, amount, duration, sourceId),
            "resistance_slashing" => CreateResistanceProperty(DamageType.Slashing, amount, duration, sourceId),
            "resistance_piercing" => CreateResistanceProperty(DamageType.Piercing, amount, duration, sourceId),
            "resistance_bludgeoning" => CreateResistanceProperty(DamageType.Bludgeoning, amount, duration, sourceId),

            // On-damaged properties
            "thorns" => CreateThornsProperty(amount, duration, sourceId),

            // Stat bonus properties
            "armor" => CreateStatBonusProperty(StatBonusType.Armor, amount, duration, sourceId),
            "evasion" => CreateStatBonusProperty(StatBonusType.Evasion, amount, duration, sourceId),
            "regen" => CreateStatBonusProperty(StatBonusType.Regen, amount, duration, sourceId),
            "max_health" => CreateStatBonusProperty(StatBonusType.MaxHealth, amount, duration, sourceId),

            _ => null
        };

        if (property == null)
        {
            GD.PrintErr($"ItemPropertyFactory: Unknown property type '{propertyType}'");
        }

        return property;
    }

    /// <summary>
    /// Checks if a property type string is valid and recognized.
    /// </summary>
    public static bool IsValidType(string? propertyType)
    {
        if (string.IsNullOrEmpty(propertyType))
            return false;

        return propertyType.ToLower() switch
        {
            // Weapon properties
            "damage" => true,
            "accuracy" => true,
            "flaming" => true,
            "freezing" => true,
            "electrified" => true,
            "venomous" => true,
            "vampiric" => true,
            // Resistance properties
            "resistance_fire" => true,
            "resistance_cold" => true,
            "resistance_lightning" => true,
            "resistance_poison" => true,
            "resistance_necrotic" => true,
            "resistance_acid" => true,
            "resistance_slashing" => true,
            "resistance_piercing" => true,
            "resistance_bludgeoning" => true,
            // On-damaged properties
            "thorns" => true,
            // Stat bonus properties
            "armor" => true,
            "evasion" => true,
            "regen" => true,
            "max_health" => true,
            _ => false
        };
    }

    /// <summary>
    /// Creates a resistance property with amount as percentage reduction.
    /// </summary>
    private static ResistanceProperty CreateResistanceProperty(DamageType damageType, int amount, string duration, string? sourceId)
    {
        // Amount is percentage: 50 = half damage (0.5 multiplier), 75 = 25% damage (0.25 multiplier)
        float multiplier = amount > 0 ? (100f - amount) / 100f : 0.5f;
        var property = new ResistanceProperty(damageType, multiplier)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }

    /// <summary>
    /// Creates a thorns property with the specified damage amount.
    /// </summary>
    private static ThornsProperty CreateThornsProperty(int amount, string duration, string? sourceId)
    {
        var property = new ThornsProperty(amount > 0 ? amount : 1)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }

    /// <summary>
    /// Creates a stat bonus property with the specified bonus type and amount.
    /// </summary>
    private static StatBonusProperty CreateStatBonusProperty(StatBonusType bonusType, int amount, string duration, string? sourceId)
    {
        var property = new StatBonusProperty(bonusType, amount > 0 ? amount : 1)
        {
            Duration = duration,
            SourceId = sourceId
        };
        if (property.IsTemporary)
        {
            property.RemainingTurns = property.ResolveDuration();
        }
        return property;
    }
}
