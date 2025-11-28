using Godot;
using System.Collections.Generic;
using PitsOfDespair.Skills;
using TargetingType = PitsOfDespair.Targeting.TargetingType;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable skill data structure.
/// Loaded from Data/Skills/*.yaml files.
/// </summary>
public class SkillDefinition
{
    /// <summary>
    /// Unique identifier for the skill (e.g., "power_attack", "fireball").
    /// Set automatically from filename during loading.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the skill (e.g., "Power Attack", "Fireball").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the skill does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Skill category: active, passive, reactive.
    /// Parsed to SkillCategory enum.
    /// </summary>
    public string Category { get; set; } = "active";

    /// <summary>
    /// How the skill selects targets: self, adjacent, tile, enemy, ally, area, line, cone.
    /// Parsed to TargetingType enum.
    /// </summary>
    public string Targeting { get; set; } = "self";

    /// <summary>
    /// Maximum range in tiles for targeted skills.
    /// </summary>
    public int Range { get; set; } = 0;

    /// <summary>
    /// Radius of the affected area for AoE skills (in tiles).
    /// </summary>
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Willpower cost to use this skill.
    /// </summary>
    public int WillpowerCost { get; set; } = 0;

    /// <summary>
    /// Stat requirements to learn this skill.
    /// </summary>
    public SkillPrerequisites Prerequisites { get; set; } = new();

    /// <summary>
    /// Trigger condition for reactive skills (e.g., "on_hit", "on_kill", "on_dodge").
    /// </summary>
    public string? Trigger { get; set; } = null;

    /// <summary>
    /// Willpower cost when a reactive skill triggers.
    /// </summary>
    public int TriggerCost { get; set; } = 0;

    /// <summary>
    /// Whether a reactive skill auto-triggers or prompts the player.
    /// </summary>
    public bool AutoTrigger { get; set; } = true;

    /// <summary>
    /// Effect definitions for this skill (processed in later phases).
    /// </summary>
    public List<SkillEffectDefinition> Effects { get; set; } = new();

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Tier for UI sorting (1-5, higher = more powerful).
    /// </summary>
    public int Tier { get; set; } = 1;

    /// <summary>
    /// Projectile type ID for ranged skills (e.g., "magic_missile", "fireball").
    /// If set, skill spawns a projectile that applies effects on impact.
    /// </summary>
    public string? Projectile { get; set; } = null;

    /// <summary>
    /// Whether this skill uses a projectile.
    /// </summary>
    public bool HasProjectile => !string.IsNullOrEmpty(Projectile);

    /// <summary>
    /// The delay cost of using this skill in aut (action time units).
    /// Standard actions cost 10 aut. Set to 0 for free/instant skills.
    /// Can be set higher for slow skills or lower for fast skills.
    /// </summary>
    public int DelayCost { get; set; } = 10; // ActionDelay.Standard

    /// <summary>
    /// Gets the skill category as an enum value.
    /// </summary>
    public SkillCategory GetCategory()
    {
        return Category?.ToLower() switch
        {
            "active" => SkillCategory.Active,
            "passive" => SkillCategory.Passive,
            "reactive" => SkillCategory.Reactive,
            _ => SkillCategory.Active
        };
    }

    /// <summary>
    /// Gets the targeting type as an enum value.
    /// </summary>
    public TargetingType GetTargetingType()
    {
        return Targeting?.ToLower() switch
        {
            "creature" => TargetingType.Creature,
            "enemy" => TargetingType.Creature,
            "ally" => TargetingType.Creature,
            "adjacent" => TargetingType.Creature,
            "melee" => TargetingType.Creature,
            "reach" => TargetingType.Creature,
            "ranged" => TargetingType.Creature,
            "cleave" => TargetingType.Cleave,
            "tile" => TargetingType.Tile,
            "area" => TargetingType.Area,
            "line" => TargetingType.Line,
            "cone" => TargetingType.Cone,
            "self" => TargetingType.Creature, // Self handled at action level
            _ => TargetingType.Creature
        };
    }

    /// <summary>
    /// Returns true if this skill targets the caster (no selection needed).
    /// </summary>
    public bool IsSelfTargeting()
    {
        return Targeting?.ToLower() == "self";
    }

    /// <summary>
    /// Gets the total stat requirement (sum of all prereqs).
    /// Useful for sorting by investment level.
    /// </summary>
    public int GetTotalPrerequisites()
    {
        return Prerequisites.Str + Prerequisites.Agi + Prerequisites.End + Prerequisites.Wil;
    }

    /// <summary>
    /// Gets a formatted string of prerequisites for display.
    /// Returns empty string if no prerequisites.
    /// </summary>
    public string GetPrerequisiteString()
    {
        var parts = new List<string>();

        if (Prerequisites.Level > 0)
            parts.Add($"LVL {Prerequisites.Level}");
        if (Prerequisites.Str > 0)
            parts.Add($"STR {Prerequisites.Str}");
        if (Prerequisites.Agi > 0)
            parts.Add($"AGI {Prerequisites.Agi}");
        if (Prerequisites.End > 0)
            parts.Add($"END {Prerequisites.End}");
        if (Prerequisites.Wil > 0)
            parts.Add($"WIL {Prerequisites.Wil}");

        return parts.Count > 0 ? string.Join(", ", parts) : "None";
    }

    /// <summary>
    /// Gets a short category indicator for display.
    /// </summary>
    public string GetCategoryIndicator()
    {
        return GetCategory() switch
        {
            SkillCategory.Active => "[ACT]",
            SkillCategory.Passive => "[PAS]",
            SkillCategory.Reactive => "[REA]",
            _ => "[???]"
        };
    }
}

/// <summary>
/// Stat prerequisites for learning a skill.
/// </summary>
public class SkillPrerequisites
{
    /// <summary>
    /// Minimum Strength required.
    /// </summary>
    public int Str { get; set; } = 0;

    /// <summary>
    /// Minimum Agility required.
    /// </summary>
    public int Agi { get; set; } = 0;

    /// <summary>
    /// Minimum Endurance required.
    /// </summary>
    public int End { get; set; } = 0;

    /// <summary>
    /// Minimum Willpower required.
    /// </summary>
    public int Wil { get; set; } = 0;

    /// <summary>
    /// Minimum character level required.
    /// </summary>
    public int Level { get; set; } = 0;

    /// <summary>
    /// Check if all prerequisites are zero (no requirements).
    /// </summary>
    public bool IsUniversal()
    {
        return Str == 0 && Agi == 0 && End == 0 && Wil == 0 && Level == 0;
    }

    /// <summary>
    /// Gets the primary stat requirement (highest single stat).
    /// Returns the stat name and value, or null if universal.
    /// </summary>
    public (string stat, int value)? GetPrimaryStat()
    {
        if (IsUniversal()) return null;

        var max = Str;
        var stat = "STR";

        if (Agi > max) { max = Agi; stat = "AGI"; }
        if (End > max) { max = End; stat = "END"; }
        if (Wil > max) { max = Wil; stat = "WIL"; }

        return (stat, max);
    }
}

/// <summary>
/// Effect definition for skills (placeholder for Phase 4).
/// </summary>
public class SkillEffectDefinition
{
    /// <summary>
    /// Effect type (e.g., "damage", "heal", "apply_condition", "stat_bonus").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the effect. Used in messages and UI.
    /// </summary>
    public string? Name { get; set; } = null;

    /// <summary>
    /// Sound effect ID to play when the effect is applied.
    /// </summary>
    public string? Sound { get; set; } = null;

    /// <summary>
    /// Steps for composite effects. If populated, effect is built as CompositeEffect.
    /// </summary>
    public System.Collections.Generic.List<PitsOfDespair.Effects.Composition.StepDefinition>? Steps { get; set; } = null;

    /// <summary>
    /// Numeric amount for the effect.
    /// </summary>
    public int Amount { get; set; } = 0;

    /// <summary>
    /// Dice notation for variable amounts (e.g., "2d6").
    /// </summary>
    public string? Dice { get; set; } = null;

    /// <summary>
    /// Duration in turns for timed effects.
    /// </summary>
    public int Duration { get; set; } = 0;

    /// <summary>
    /// Condition type for status effects.
    /// </summary>
    public string? ConditionType { get; set; } = null;

    /// <summary>
    /// Target stat for stat_bonus effects (e.g., "str", "max_hp", "armor").
    /// </summary>
    public string? Stat { get; set; } = null;

    /// <summary>
    /// Stat to scale effect with (e.g., "str", "wil").
    /// </summary>
    public string? ScalingStat { get; set; } = null;

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

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
    /// Amount of armor to ignore when dealing damage.
    /// </summary>
    public int ArmorPiercing { get; set; } = 0;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3", "1d4+2"). Overrides Duration if specified.
    /// </summary>
    public string? DurationDice { get; set; } = null;

    /// <summary>
    /// Dice notation for damage-over-time effects (e.g., "1d3" for acid DoT).
    /// </summary>
    public string? DotDamage { get; set; } = null;

    /// <summary>
    /// Damage type for damage effects (used for resistance/vulnerability checks).
    /// </summary>
    public string? DamageType { get; set; } = null;
}
