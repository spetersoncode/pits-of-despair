using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects;

/// <summary>
/// Unified context for all effect execution.
/// Provides everything effects need to apply themselves, regardless of source (items, skills, on-hit, etc.).
/// </summary>
public class EffectContext
{
    /// <summary>
    /// The entity receiving the effect.
    /// </summary>
    public BaseEntity Target { get; }

    /// <summary>
    /// The entity causing the effect (null for environmental effects or items without a wielder).
    /// </summary>
    public BaseEntity? Caster { get; }

    /// <summary>
    /// The action context with game systems (map, entity manager, combat, etc.).
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Optional skill definition for skill-based effects (provides scaling info).
    /// </summary>
    public SkillDefinition? Skill { get; }

    /// <summary>
    /// The caster's stats component (cached for scaling calculations).
    /// </summary>
    public StatsComponent? CasterStats { get; }

    /// <summary>
    /// Optional target position for position-based effects (e.g., directional movement).
    /// Used when the effect needs direction rather than a target entity.
    /// </summary>
    public GridPosition? TargetPosition { get; }

    /// <summary>
    /// Whether this effect comes from a skill (vs item, trap, etc.).
    /// </summary>
    public bool IsSkillEffect => Skill != null;

    private EffectContext(
        BaseEntity target,
        BaseEntity? caster,
        ActionContext actionContext,
        SkillDefinition? skill = null,
        GridPosition? targetPosition = null)
    {
        Target = target;
        Caster = caster;
        ActionContext = actionContext;
        Skill = skill;
        CasterStats = caster?.GetNodeOrNull<StatsComponent>("StatsComponent");
        TargetPosition = targetPosition;
    }

    /// <summary>
    /// Creates an effect context for item usage.
    /// Caster is the entity using the item.
    /// </summary>
    public static EffectContext ForItem(BaseEntity target, BaseEntity user, ActionContext actionContext)
    {
        return new EffectContext(target, user, actionContext);
    }

    /// <summary>
    /// Creates an effect context for skill execution.
    /// Includes skill definition for scaling calculations.
    /// </summary>
    public static EffectContext ForSkill(
        BaseEntity target,
        BaseEntity caster,
        ActionContext actionContext,
        SkillDefinition skill,
        GridPosition? targetPosition = null)
    {
        return new EffectContext(target, caster, actionContext, skill, targetPosition);
    }

    /// <summary>
    /// Creates an effect context for environmental effects (traps, hazards).
    /// No caster is specified.
    /// </summary>
    public static EffectContext ForEnvironment(BaseEntity target, ActionContext actionContext)
    {
        return new EffectContext(target, null, actionContext);
    }

    /// <summary>
    /// Creates an effect context for on-hit effects (weapon procs, reactive skills).
    /// </summary>
    public static EffectContext ForOnHit(
        BaseEntity target,
        BaseEntity attacker,
        ActionContext actionContext,
        SkillDefinition? skill = null)
    {
        return new EffectContext(target, attacker, actionContext, skill);
    }

    /// <summary>
    /// Gets a stat value from the caster for effect scaling.
    /// Returns 0 if no caster or stats component.
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

    /// <summary>
    /// Gets the display name for the effect source (skill name, "item", etc.).
    /// Useful for failure messages.
    /// </summary>
    public string GetSourceName()
    {
        if (Skill != null)
            return Skill.Name;
        return "effect";
    }
}
