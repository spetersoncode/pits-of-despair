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

    /// <summary>
    /// Whether to use melee attack modifier (STR + AGI) instead of a stat.
    /// </summary>
    public bool UseMeleeModifier { get; set; } = false;

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

    /// <summary>
    /// Range in tiles for teleport/blink effects.
    /// 0 = unlimited range (full map).
    /// </summary>
    public int Range { get; set; } = 0;

    /// <summary>
    /// Radius in tiles for area effects (magic mapping).
    /// </summary>
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Whether to teleport companions when the target is the player.
    /// </summary>
    public bool TeleportCompanions { get; set; } = true;

    #endregion

    #region Hazard Properties

    /// <summary>
    /// Type of hazard to create (e.g., "poison_cloud", "fire").
    /// </summary>
    public string? HazardType { get; set; }

    #endregion

    #region Prime Attack Properties

    /// <summary>
    /// Name of the primed attack (e.g., "Power Attack").
    /// </summary>
    public string? PrimeName { get; set; }

    /// <summary>
    /// Bonus to attack roll when prime triggers.
    /// </summary>
    public int HitBonus { get; set; } = 0;

    /// <summary>
    /// Bonus to damage when prime triggers.
    /// </summary>
    public int DamageBonus { get; set; } = 0;

    /// <summary>
    /// Targeting mode for primed attacks: "single" (default) or "arc" (cleave-style 3-tile arc).
    /// </summary>
    public string? TargetingMode { get; set; }

    #endregion

    #region Chain Damage Properties

    /// <summary>
    /// Maximum number of bounces for chain effects.
    /// </summary>
    public int MaxBounces { get; set; } = 3;

    /// <summary>
    /// Range in tiles to search for bounce targets.
    /// </summary>
    public int BounceRange { get; set; } = 4;

    /// <summary>
    /// Damage multiplier per bounce (e.g., 0.75 = 75% of previous).
    /// </summary>
    public float DamageFalloff { get; set; } = 1.0f;

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
