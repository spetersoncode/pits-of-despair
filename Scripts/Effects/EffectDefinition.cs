using System.Collections.Generic;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;

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
    /// For composite effects, this is the identifier for the composed effect.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the effect. Used in messages and UI.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Sound effect ID to play when the effect is applied.
    /// Maps to effect_sounds.yaml registry.
    /// </summary>
    public string? Sound { get; set; }

    /// <summary>
    /// Steps for composite effects. If populated, effect is built as CompositeEffect.
    /// </summary>
    public List<StepDefinition>? Steps { get; set; }

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
    /// Target's save stat for saving throw (e.g., "end" for physical, "wil" for mental).
    /// If set, a saving throw is required before the effect applies.
    /// </summary>
    public string? SaveStat { get; set; } = null;

    /// <summary>
    /// Caster's attack stat for the opposed saving throw roll.
    /// Defaults to "wil" if SaveStat is set but AttackStat is not.
    /// </summary>
    public string? AttackStat { get; set; } = null;

    /// <summary>
    /// Modifier to the caster's save roll. Positive = harder to resist, negative = easier.
    /// </summary>
    public int SaveModifier { get; set; } = 0;

    /// <summary>
    /// Maximum number of bounces for chain effects (e.g., Chain Lightning).
    /// </summary>
    public int MaxBounces { get; set; } = 3;

    /// <summary>
    /// Range (in tiles) that chain effects can bounce to nearby targets.
    /// </summary>
    public int BounceRange { get; set; } = 4;

    /// <summary>
    /// Damage multiplier applied to each subsequent bounce (e.g., 0.75 = 75% damage per bounce).
    /// </summary>
    public float DamageFalloff { get; set; } = 1.0f;

    /// <summary>
    /// Dice notation for damage-over-time effects (e.g., "1d3" for acid DoT).
    /// </summary>
    public string? DotDamage { get; set; } = null;

    /// <summary>
    /// Amount of armor to ignore when dealing damage.
    /// </summary>
    public int ArmorPiercing { get; set; } = 0;

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
            Name = skillEffect.Name,
            Sound = skillEffect.Sound,
            Steps = skillEffect.Steps,
            Amount = skillEffect.Amount,
            Dice = skillEffect.Dice,
            Duration = skillEffect.Duration,
            DurationDice = skillEffect.DurationDice,
            ConditionType = skillEffect.ConditionType,
            Stat = skillEffect.Stat,
            ScalingStat = skillEffect.ScalingStat,
            ScalingMultiplier = skillEffect.ScalingMultiplier,
            Targets = skillEffect.Targets,
            Percent = skillEffect.Percent,
            SaveStat = skillEffect.SaveStat,
            AttackStat = skillEffect.AttackStat,
            SaveModifier = skillEffect.SaveModifier,
            MaxBounces = skillEffect.MaxBounces,
            BounceRange = skillEffect.BounceRange,
            DamageFalloff = skillEffect.DamageFalloff,
            DotDamage = skillEffect.DotDamage,
            DamageType = skillEffect.DamageType,
            ArmorPiercing = skillEffect.ArmorPiercing
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
            Name = itemEffect.Name,
            Sound = itemEffect.Sound,
            Steps = itemEffect.Steps,
            Amount = itemEffect.Amount,
            Range = itemEffect.Range,
            Duration = itemEffect.Duration,
            DurationDice = itemEffect.DurationDice,
            ConditionType = itemEffect.ConditionType,
            Dice = itemEffect.Dice,
            Radius = itemEffect.Radius,
            DamageType = itemEffect.DamageType,
            HazardType = itemEffect.HazardType,
            SaveStat = itemEffect.SaveStat,
            AttackStat = itemEffect.AttackStat,
            SaveModifier = itemEffect.SaveModifier,
            DotDamage = itemEffect.DotDamage,
            ArmorPiercing = itemEffect.ArmorPiercing,
            ScalingStat = itemEffect.ScalingStat,
            ScalingMultiplier = itemEffect.ScalingMultiplier
        };
    }
}
