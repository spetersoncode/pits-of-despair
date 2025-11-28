using YamlDotNet.Serialization;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// YAML configuration for a single step in a composite effect.
/// Contains all possible properties - steps use only what they need.
/// </summary>
public class StepDefinition
{
    /// <summary>
    /// Step type identifier (e.g., "damage", "save_check", "apply_condition").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    #region Dice and Amounts

    /// <summary>
    /// Dice notation for the step (e.g., "2d6", "1d8+2").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Fixed numeric amount.
    /// </summary>
    public int Amount { get; set; } = 0;

    /// <summary>
    /// Percentage value (e.g., 0.5 for 50% healing from damage dealt).
    /// </summary>
    public float Fraction { get; set; } = 0f;

    /// <summary>
    /// Whether Amount is a percentage of max value.
    /// </summary>
    public bool Percent { get; set; } = false;

    #endregion

    #region Damage Properties

    /// <summary>
    /// Damage type (e.g., "Fire", "Acid", "Necrotic").
    /// </summary>
    public string? DamageType { get; set; }

    /// <summary>
    /// Amount of armor to ignore.
    /// </summary>
    public int ArmorPiercing { get; set; } = 0;

    /// <summary>
    /// Whether damage is halved on a successful save.
    /// </summary>
    public bool HalfOnSave { get; set; } = false;

    #endregion

    #region Save/Attack Properties

    /// <summary>
    /// Target's save stat (e.g., "end", "wil").
    /// </summary>
    public string? SaveStat { get; set; }

    /// <summary>
    /// Caster's attack stat for opposed rolls.
    /// </summary>
    public string? AttackStat { get; set; }

    /// <summary>
    /// Modifier to the caster's roll.
    /// </summary>
    public int SaveModifier { get; set; } = 0;

    /// <summary>
    /// Whether to stop the pipeline on save success.
    /// </summary>
    public bool StopOnSuccess { get; set; } = false;

    /// <summary>
    /// Whether target takes half damage on save success.
    /// </summary>
    public bool HalfOnSuccess { get; set; } = false;

    /// <summary>
    /// Whether to stop the pipeline on attack miss.
    /// </summary>
    public bool StopOnMiss { get; set; } = true;

    #endregion

    #region Condition Properties

    /// <summary>
    /// Condition type to apply (e.g., "acid", "confusion").
    /// </summary>
    public string? ConditionType { get; set; }

    /// <summary>
    /// Fixed duration in turns.
    /// </summary>
    public int Duration { get; set; } = 0;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3").
    /// </summary>
    public string? DurationDice { get; set; }

    /// <summary>
    /// Dice notation for DoT damage (e.g., "1d3").
    /// </summary>
    public string? DotDamage { get; set; }

    /// <summary>
    /// Only apply condition if target failed a save.
    /// </summary>
    public bool RequireSaveFailed { get; set; } = false;

    /// <summary>
    /// Only apply condition if damage was dealt.
    /// </summary>
    public bool RequireDamageDealt { get; set; } = false;

    #endregion

    #region Scaling Properties

    /// <summary>
    /// Stat to scale amount with (e.g., "str", "wil").
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    #endregion

    #region Movement Properties

    /// <summary>
    /// Distance in tiles for knockback effects.
    /// </summary>
    public int Distance { get; set; } = 1;

    #endregion

    /// <summary>
    /// Gets the duration string, preferring DurationDice over Duration.
    /// </summary>
    public string GetDurationString()
    {
        if (!string.IsNullOrEmpty(DurationDice))
            return DurationDice;
        return Duration > 0 ? Duration.ToString() : "1";
    }
}
