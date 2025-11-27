using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.Audio;
using System.Collections.Generic;

namespace PitsOfDespair.Effects;

/// <summary>
/// Base class for all effects in the game.
/// Effects can be applied by items, skills, on-hit procs, traps, or environmental hazards.
/// This is the unified effect system - all effect implementations inherit from this class.
/// </summary>
public abstract class Effect
{
    /// <summary>
    /// The type identifier for this effect (e.g., "damage", "heal", "apply_condition").
    /// Used for serialization and factory creation.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Display name for this effect (defaults to Type if not overridden).
    /// </summary>
    public virtual string Name => Type;

    /// <summary>
    /// Applies this effect using the unified context.
    /// Target, caster, and game systems are all accessible via context.
    /// </summary>
    /// <param name="context">The effect context containing target, caster, and game systems.</param>
    /// <returns>The result of applying the effect.</returns>
    public abstract EffectResult Apply(EffectContext context);

    /// <summary>
    /// Legacy method for backward compatibility during migration.
    /// Wraps the old signature into the new context-based approach.
    /// </summary>
    [System.Obsolete("Use Apply(EffectContext) instead. This method exists for backward compatibility.")]
    public EffectResult Apply(BaseEntity target, ActionContext actionContext)
    {
        var context = EffectContext.ForItem(target, target, actionContext);
        return Apply(context);
    }

    /// <summary>
    /// Creates an effect from a unified effect definition.
    /// </summary>
    public static Effect? CreateFromDefinition(EffectDefinition definition)
    {
        return definition.Type?.ToLower() switch
        {
            "heal" or "modify_health" => new ModifyHealthEffect(definition),
            "heal_percent" => new ModifyHealthEffect(definition) { Percent = true },
            "damage" => new DamageEffect(definition),
            "magic_missile" => new MagicMissileEffect(definition),
            "apply_condition" => new ApplyConditionEffect(definition),
            "teleport" => new TeleportEffect(definition),
            "blink" => new BlinkEffect(definition),
            "knockback" => new KnockbackEffect(definition),
            "restore_willpower" or "modify_willpower" => new ModifyWillpowerEffect(definition),
            "charm" => new CharmEffect(),
            "fireball" => new FireballEffect(definition),
            "tunneling" => new TunnelingEffect(definition),
            "vampiric_touch" => new VampiricTouchEffect(definition),
            "create_hazard" => new CreateHazardEffect(definition),
            "clone" => new CloneEffect(definition),
            "cone_of_cold" => new ConeOfColdEffect(definition),
            "move_tiles" => new MoveTilesEffect(definition),
            "melee_attack" => new MeleeAttackEffect(definition),
            "lightning_bolt" => new LightningBoltEffect(definition),
            "chain_lightning" => new ChainLightningEffect(definition),
            "fear" => new FearEffect(definition),
            "sleep" => new SleepEffect(definition),
            "magic_mapping" => new MagicMappingEffect(definition),
            "acid" => new AcidEffect(definition),
            _ => null
        };
    }

    /// <summary>
    /// Creates an effect from a skill effect definition.
    /// Maps skill effect definitions to unified effects.
    /// </summary>
    public static Effect? CreateFromSkillDefinition(SkillEffectDefinition definition)
    {
        // Convert SkillEffectDefinition to unified EffectDefinition
        var effectDef = EffectDefinition.FromSkillEffect(definition);
        return CreateFromDefinition(effectDef);
    }

    /// <summary>
    /// Helper to calculate a scaled amount with optional dice and stat scaling.
    /// Common pattern used by damage, healing, and other scaled effects.
    /// </summary>
    protected int CalculateScaledAmount(int baseAmount, string? dice, string? scalingStat, float scalingMultiplier, EffectContext context)
    {
        int amount = baseAmount;

        // Add dice roll if specified
        if (!string.IsNullOrEmpty(dice))
        {
            amount += DiceRoller.Roll(dice);
        }

        // Add stat scaling if we have a caster
        if (!string.IsNullOrEmpty(scalingStat) && context.Caster != null)
        {
            int statValue = context.GetCasterStat(scalingStat);
            amount += (int)(statValue * scalingMultiplier);
        }

        return amount;
    }

    /// <summary>
    /// Plays the sound associated with this effect type (if one is registered).
    /// </summary>
    protected void PlayEffectSound()
    {
        AudioManager.PlayEffectSound(Type);
    }

    /// <summary>
    /// Applies this effect to multiple targets.
    /// Handles audio playback centrally and iterates through all targets.
    /// </summary>
    /// <param name="caster">The entity applying the effect.</param>
    /// <param name="targets">List of target entities.</param>
    /// <param name="context">The action context.</param>
    /// <returns>List of results for each target.</returns>
    public virtual List<EffectResult> ApplyToTargets(BaseEntity caster, List<BaseEntity> targets, ActionContext context)
    {
        PlayEffectSound();

        var results = new List<EffectResult>();
        foreach (var target in targets)
        {
            var effectContext = EffectContext.ForItem(target, caster, context);
            results.Add(Apply(effectContext));
        }
        return results;
    }
}

/// <summary>
/// Represents the result of applying an effect.
/// Unified result type for all effect sources (items, skills, on-hit, etc.).
/// </summary>
public class EffectResult
{
    /// <summary>
    /// Whether the effect was successfully applied.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing what happened when the effect was applied.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Optional color for the message (hex format for BBCode).
    /// </summary>
    public string MessageColor { get; set; }

    /// <summary>
    /// The entity affected by this effect (if applicable).
    /// Useful for chaining effects or tracking what was hit.
    /// </summary>
    public BaseEntity? AffectedEntity { get; set; }

    /// <summary>
    /// Amount of damage dealt (for damage effects).
    /// Used by callers to emit damage signals for message log.
    /// </summary>
    public int DamageDealt { get; set; }

    public EffectResult(bool success, string message, string? messageColor = null)
    {
        Success = success;
        Message = message;
        MessageColor = messageColor ?? Palette.ToHex(Palette.Default);
    }

    /// <summary>
    /// Creates a successful effect result.
    /// </summary>
    public static EffectResult CreateSuccess(string message, string? color = null, BaseEntity? affected = null)
    {
        return new EffectResult(true, message, color) { AffectedEntity = affected };
    }

    /// <summary>
    /// Creates a failed effect result.
    /// </summary>
    public static EffectResult CreateFailure(string message, string? color = null)
    {
        return new EffectResult(false, message, color);
    }
}
