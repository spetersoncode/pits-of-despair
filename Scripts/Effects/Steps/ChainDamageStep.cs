using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.VisualEffects;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that performs bouncing damage between enemies.
/// Hits the initial target, then chains to nearby enemies up to MaxBounces times.
/// </summary>
public class ChainDamageStep : IEffectStep
{
    private readonly int _amount;
    private readonly string? _dice;
    private readonly DamageType _damageType;
    private readonly int _maxBounces;
    private readonly int _bounceRange;
    private readonly float _damageFalloff;

    public ChainDamageStep(StepDefinition definition)
    {
        _amount = definition.Amount;
        _dice = definition.Dice;
        _maxBounces = definition.MaxBounces > 0 ? definition.MaxBounces : 3;
        _bounceRange = definition.BounceRange > 0 ? definition.BounceRange : 4;
        _damageFalloff = definition.DamageFalloff > 0 ? definition.DamageFalloff : 1.0f;

        _damageType = DamageType.Lightning;
        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                _damageType = dt;
            }
        }
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var caster = context.Caster;
        var actionContext = context.ActionContext;

        if (caster == null || actionContext == null)
        {
            messages.Add("Chain damage requires a caster.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Calculate base damage once
        int baseDamage = _amount;
        if (!string.IsNullOrEmpty(_dice))
        {
            baseDamage += DiceRoller.Roll(_dice);
        }
        baseDamage = System.Math.Max(1, baseDamage);

        // Track hit entities
        var hitEntities = new HashSet<BaseEntity>();

        // Start the chain
        BaseEntity? currentTarget = target;
        GridPosition previousPosition = caster.GridPosition;
        int currentDamage = baseDamage;
        int bounceCount = 0;
        int totalDamage = 0;

        while (currentTarget != null && bounceCount <= _maxBounces)
        {
            // Skip already-hit entities
            if (hitEntities.Contains(currentTarget))
            {
                break;
            }

            hitEntities.Add(currentTarget);

            // Spawn visual effect from previous position to current target
            if (actionContext.VisualEffectSystem != null)
            {
                actionContext.VisualEffectSystem.SpawnProjectile(
                    VisualEffectDefinitions.Spark,
                    previousPosition,
                    currentTarget.GridPosition,
                    null
                );
            }

            // Apply damage to current target
            var healthComponent = currentTarget.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (healthComponent != null)
            {
                int actualDamage = healthComponent.TakeDamage(currentDamage, _damageType, caster);
                totalDamage += actualDamage;

                string message = bounceCount == 0
                    ? $"{currentTarget.DisplayName} is struck by chain lightning for {actualDamage} damage!"
                    : $"Lightning arcs to {currentTarget.DisplayName} for {actualDamage} damage!";

                messages.Add(message, Palette.ToHex(Palette.Lightning));

                // Emit damage signal for message log on bounces
                if (bounceCount > 0 && actionContext.CombatSystem != null)
                {
                    actionContext.CombatSystem.EmitSkillDamageDealt(caster, currentTarget, actualDamage, "Chain Lightning");
                }
            }

            // Find next target for bounce
            previousPosition = currentTarget.GridPosition;
            currentTarget = FindNextBounceTarget(currentTarget.GridPosition, caster, hitEntities, actionContext);
            bounceCount++;

            // Apply damage falloff for next bounce
            currentDamage = (int)(currentDamage * _damageFalloff);
            if (currentDamage < 1)
            {
                currentDamage = 1;
            }
        }

        if (hitEntities.Count == 0)
        {
            messages.Add("The chain lightning crackles but finds no target.", Palette.ToHex(Palette.Lightning));
        }
        else
        {
            state.DamageDealt = totalDamage;
            state.Success = true;
        }
    }

    private BaseEntity? FindNextBounceTarget(
        GridPosition fromPosition,
        BaseEntity caster,
        HashSet<BaseEntity> alreadyHit,
        Actions.ActionContext context)
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
                       DistanceHelper.IsInChebyshevRange(fromPosition, e.GridPosition, _bounceRange))
            .OrderBy(e => DistanceHelper.ChebyshevDistance(fromPosition, e.GridPosition))
            .ToList();

        return validTargets.FirstOrDefault();
    }
}
