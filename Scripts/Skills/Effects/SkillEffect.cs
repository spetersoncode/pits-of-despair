using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Skills.Effects;

/// <summary>
/// Context passed to skill effects during execution.
/// Contains caster info, targets, and game systems.
/// </summary>
public class SkillEffectContext
{
    /// <summary>
    /// The entity casting the skill.
    /// </summary>
    public BaseEntity Caster { get; }

    /// <summary>
    /// The action context with game systems.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// The skill being executed.
    /// </summary>
    public SkillDefinition Skill { get; }

    /// <summary>
    /// The caster's stats component (cached for scaling calculations).
    /// </summary>
    public StatsComponent? CasterStats { get; }

    public SkillEffectContext(BaseEntity caster, ActionContext actionContext, SkillDefinition skill)
    {
        Caster = caster;
        ActionContext = actionContext;
        Skill = skill;
        CasterStats = caster.GetNodeOrNull<StatsComponent>("StatsComponent");
    }

    /// <summary>
    /// Gets a stat value from the caster for effect scaling.
    /// </summary>
    /// <param name="statName">Stat name: "str", "agi", "end", "wil"</param>
    /// <returns>The stat value, or 0 if not found</returns>
    public int GetCasterStat(string? statName)
    {
        if (CasterStats == null || string.IsNullOrEmpty(statName))
            return 0;

        return statName.ToLower() switch
        {
            "str" => CasterStats.TotalStrength,
            "agi" => CasterStats.TotalAgility,
            "end" => CasterStats.TotalEndurance,
            "wil" => CasterStats.TotalWill,
            _ => 0
        };
    }
}

/// <summary>
/// Result of applying a skill effect to a target.
/// </summary>
public class SkillEffectResult
{
    /// <summary>
    /// Whether the effect was successfully applied.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing what happened.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Color for the message (hex format for BBCode).
    /// </summary>
    public string MessageColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// The entity affected by this effect (if applicable).
    /// </summary>
    public BaseEntity? AffectedEntity { get; set; }

    public SkillEffectResult(bool success, string message, string? messageColor = null)
    {
        Success = success;
        Message = message;
        MessageColor = messageColor ?? "#FFFFFF";
    }

    /// <summary>
    /// Creates a successful effect result.
    /// </summary>
    public static SkillEffectResult CreateSuccess(string message, string? color = null, BaseEntity? affected = null)
    {
        return new SkillEffectResult(true, message, color) { AffectedEntity = affected };
    }

    /// <summary>
    /// Creates a failed effect result.
    /// </summary>
    public static SkillEffectResult CreateFailure(string message, string? color = null)
    {
        return new SkillEffectResult(false, message, color);
    }
}

/// <summary>
/// Base class for all skill effects.
/// Effects are instantiated from SkillEffectDefinition data and applied to targets.
/// </summary>
public abstract class SkillEffect
{
    /// <summary>
    /// The type name of this effect (e.g., "damage", "heal").
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Applies this effect to a single target.
    /// </summary>
    /// <param name="target">The entity receiving the effect</param>
    /// <param name="context">The skill execution context</param>
    /// <returns>The result of applying the effect</returns>
    public abstract SkillEffectResult Apply(BaseEntity target, SkillEffectContext context);

    /// <summary>
    /// Creates a skill effect from a definition.
    /// </summary>
    /// <param name="definition">The effect definition from skill data</param>
    /// <returns>A new SkillEffect instance, or null if type is unknown</returns>
    public static SkillEffect? CreateFromDefinition(SkillEffectDefinition definition)
    {
        return definition.Type?.ToLower() switch
        {
            "damage" => new DamageSkillEffect(definition),
            "heal" => new HealSkillEffect(definition),
            "restore_willpower" => new RestoreWillpowerEffect(definition),
            "apply_condition" => new ApplyConditionSkillEffect(definition),
            "teleport" => new TeleportEffect(definition),
            "knockback" => new KnockbackEffect(definition),
            _ => null
        };
    }
}
