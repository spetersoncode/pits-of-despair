using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.VisualEffects;

namespace PitsOfDespair.Effects;

/// <summary>
/// Chain lightning effect that bounces between nearby enemies.
/// Hits the initial target, then chains to nearby enemies up to MaxBounces times.
/// Damage can optionally fall off with each bounce.
/// </summary>
public class ChainLightningEffect : Effect
{
    public override string Type => "chain_lightning";
    public override string Name => "Chain Lightning";

    /// <summary>
    /// Flat damage amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable damage (e.g., "2d4").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Damage type (defaults to Lightning).
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Lightning;

    /// <summary>
    /// Maximum number of times the lightning can bounce to new targets.
    /// </summary>
    public int MaxBounces { get; set; } = 3;

    /// <summary>
    /// Maximum range (in tiles) to search for bounce targets.
    /// </summary>
    public int BounceRange { get; set; } = 4;

    /// <summary>
    /// Damage multiplier applied to each subsequent bounce (e.g., 0.75 = 75% of previous damage).
    /// </summary>
    public float DamageFalloff { get; set; } = 1.0f;

    public ChainLightningEffect() { }

    public ChainLightningEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        MaxBounces = definition.MaxBounces;
        BounceRange = definition.BounceRange;
        DamageFalloff = definition.DamageFalloff;

        // Parse damage type if specified
        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                DamageType = dt;
            }
        }
    }

    /// <summary>
    /// Applies chain lightning to the initial target.
    /// This is the entry point - it will handle the initial hit and all bounces.
    /// </summary>
    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var caster = context.Caster;
        var actionContext = context.ActionContext;

        if (caster == null || actionContext == null)
        {
            return EffectResult.CreateFailure(
                "Chain lightning requires a caster.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply the chain effect starting from the initial target
        var results = ApplyChain(caster, target, actionContext);

        // Combine results into a single result
        if (results.Count == 0)
        {
            return EffectResult.CreateFailure(
                "The lightning fizzles out.",
                Palette.ToHex(Palette.Lightning)
            );
        }

        // Return the first result (initial hit) - other results are logged separately
        return results[0];
    }

    /// <summary>
    /// Applies the chain lightning effect, bouncing between targets.
    /// </summary>
    /// <param name="caster">The entity casting the effect.</param>
    /// <param name="initialTarget">The first target to hit.</param>
    /// <param name="context">The action context.</param>
    /// <returns>List of effect results for each entity hit.</returns>
    public List<EffectResult> ApplyChain(BaseEntity caster, BaseEntity initialTarget, ActionContext context)
    {
        var results = new List<EffectResult>();
        var hitEntities = new HashSet<BaseEntity>();

        // Calculate base damage once
        int baseDamage = Amount;
        if (!string.IsNullOrEmpty(Dice))
        {
            baseDamage += DiceRoller.Roll(Dice);
        }
        baseDamage = System.Math.Max(1, baseDamage);

        // Start the chain
        BaseEntity currentTarget = initialTarget;
        GridPosition previousPosition = caster.GridPosition;
        int currentDamage = baseDamage;
        int bounceCount = 0;

        while (currentTarget != null && bounceCount <= MaxBounces)
        {
            // Skip already-hit entities
            if (hitEntities.Contains(currentTarget))
            {
                break;
            }

            hitEntities.Add(currentTarget);

            // Spawn visual effect from previous position to current target
            if (context.VisualEffectSystem != null)
            {
                // Use spark projectile for chain arcs
                context.VisualEffectSystem.SpawnProjectile(
                    VisualEffectDefinitions.Spark,
                    previousPosition,
                    currentTarget.GridPosition,
                    null // No callback needed - damage is applied immediately
                );
            }

            // Apply damage to current target
            var healthComponent = currentTarget.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (healthComponent != null)
            {
                int actualDamage = healthComponent.TakeDamage(currentDamage, DamageType, caster);

                string message = bounceCount == 0
                    ? $"{currentTarget.DisplayName} is struck by chain lightning for {actualDamage} damage!"
                    : $"Lightning arcs to {currentTarget.DisplayName} for {actualDamage} damage!";

                var result = EffectResult.CreateSuccess(
                    message,
                    Palette.ToHex(Palette.Lightning),
                    currentTarget
                );
                result.DamageDealt = actualDamage;
                results.Add(result);

                // Emit damage signal for message log
                if (bounceCount > 0 && context.CombatSystem != null)
                {
                    context.CombatSystem.EmitSkillDamageDealt(caster, currentTarget, actualDamage, "Chain Lightning");
                }
            }

            // Find next target for bounce
            previousPosition = currentTarget.GridPosition;
            currentTarget = FindNextBounceTarget(currentTarget.GridPosition, caster, hitEntities, context);
            bounceCount++;

            // Apply damage falloff for next bounce
            currentDamage = (int)(currentDamage * DamageFalloff);
            if (currentDamage < 1)
            {
                currentDamage = 1;
            }
        }

        // If no targets were hit, add a failure message
        if (results.Count == 0)
        {
            results.Add(EffectResult.CreateSuccess(
                "The chain lightning crackles but finds no target.",
                Palette.ToHex(Palette.Lightning)
            ));
        }

        return results;
    }

    /// <summary>
    /// Finds the nearest valid enemy within bounce range that hasn't been hit yet.
    /// </summary>
    private BaseEntity? FindNextBounceTarget(
        GridPosition fromPosition,
        BaseEntity caster,
        HashSet<BaseEntity> alreadyHit,
        ActionContext context)
    {
        if (context.EntityManager == null)
        {
            return null;
        }

        var allEntities = context.EntityManager.GetAllEntities();

        // Find valid targets: enemies with health, in range, not already hit
        var validTargets = allEntities
            .Where(e => !alreadyHit.Contains(e) &&
                       !e.IsDead &&
                       e.Faction != caster.Faction &&
                       e.GetNodeOrNull<HealthComponent>("HealthComponent") != null &&
                       DistanceHelper.IsInChebyshevRange(fromPosition, e.GridPosition, BounceRange))
            .OrderBy(e => DistanceHelper.ChebyshevDistance(fromPosition, e.GridPosition))
            .ToList();

        return validTargets.FirstOrDefault();
    }
}
