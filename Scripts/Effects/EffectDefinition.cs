using PitsOfDespair.Data;

namespace PitsOfDespair.Effects;

/// <summary>
/// Unified effect definition that can be created from item YAML or skill YAML.
/// Contains all parameters needed to instantiate any effect type.
/// This is the adapter layer between different data sources (items, skills) and the unified Effect system.
/// </summary>
public class EffectDefinition
{
    /// <summary>
    /// The type of effect (e.g., "heal", "damage", "teleport", "apply_condition").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Numeric parameter for the effect (e.g., heal amount, damage amount, knockback distance).
    /// </summary>
    public int Amount { get; set; } = 0;

    /// <summary>
    /// Range parameter for area/distance effects (e.g., teleport range, blink range).
    /// </summary>
    public int Range { get; set; } = 0;

    /// <summary>
    /// Dice notation for variable amounts (e.g., "2d6", "1d8+2").
    /// </summary>
    public string? Dice { get; set; } = null;

    /// <summary>
    /// Duration in turns for condition effects. Used if DurationDice is not set.
    /// </summary>
    public int Duration { get; set; } = 0;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3", "1d4+2"). Overrides Duration if specified.
    /// </summary>
    public string? DurationDice { get; set; } = null;

    /// <summary>
    /// Condition type for apply_condition effects (e.g., "confusion", "armor_buff").
    /// </summary>
    public string? ConditionType { get; set; } = null;

    /// <summary>
    /// Target stat for stat_bonus effects (e.g., "str", "max_hp", "armor").
    /// </summary>
    public string? Stat { get; set; } = null;

    /// <summary>
    /// Stat to scale effect amount with (e.g., "str", "wil").
    /// </summary>
    public string? ScalingStat { get; set; } = null;

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Damage type for damage effects (used for resistance/vulnerability checks).
    /// </summary>
    public string? DamageType { get; set; } = null;

    /// <summary>
    /// Area radius for AOE effects (in tiles, Euclidean distance).
    /// </summary>
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Hazard type for create_hazard effects (e.g., "poison_cloud", "fire").
    /// </summary>
    public string? HazardType { get; set; } = null;

    /// <summary>
    /// Number of targets for multi-target effects (e.g., Cleave hits 2 enemies).
    /// </summary>
    public int Targets { get; set; } = 1;

    /// <summary>
    /// Whether the Amount is a percentage (e.g., heal 20% of max HP).
    /// </summary>
    public bool Percent { get; set; } = false;

    /// <summary>
    /// Gets the resolved duration string (prefers DurationDice over Duration).
    /// </summary>
    public string GetDurationString()
    {
        if (!string.IsNullOrEmpty(DurationDice))
            return DurationDice;
        return Duration.ToString();
    }

    /// <summary>
    /// Creates an EffectDefinition from a SkillEffectDefinition.
    /// Maps skill-specific fields to the unified definition.
    /// </summary>
    public static EffectDefinition FromSkillEffect(SkillEffectDefinition skillEffect)
    {
        return new EffectDefinition
        {
            Type = skillEffect.Type,
            Amount = skillEffect.Amount,
            Dice = skillEffect.Dice,
            Duration = skillEffect.Duration,
            ConditionType = skillEffect.ConditionType,
            Stat = skillEffect.Stat,
            ScalingStat = skillEffect.ScalingStat,
            ScalingMultiplier = skillEffect.ScalingMultiplier,
            Targets = skillEffect.Targets,
            Percent = skillEffect.Percent
        };
    }

    /// <summary>
    /// Creates an EffectDefinition from item YAML effect data.
    /// This is for use when ItemData's own EffectDefinition needs to be converted.
    /// </summary>
    public static EffectDefinition FromItemEffect(Data.EffectDefinition itemEffect)
    {
        return new EffectDefinition
        {
            Type = itemEffect.Type,
            Amount = itemEffect.Amount,
            Range = itemEffect.Range,
            Duration = itemEffect.Duration,
            DurationDice = itemEffect.DurationDice,
            ConditionType = itemEffect.ConditionType,
            Dice = itemEffect.Dice,
            Radius = itemEffect.Radius,
            DamageType = itemEffect.DamageType,
            HazardType = itemEffect.HazardType
        };
    }
}
