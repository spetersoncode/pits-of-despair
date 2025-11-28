using System;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that deals damage to the target.
/// Supports dice, scaling, armor piercing, and half damage on save.
/// </summary>
public class DamageStep : IEffectStep
{
    private readonly string? _dice;
    private readonly int _amount;
    private readonly DamageType _damageType;
    private readonly int _armorPiercing;
    private readonly bool _halfOnSave;
    private readonly string? _scalingStat;
    private readonly float _scalingMultiplier;

    public DamageStep(StepDefinition definition)
    {
        _dice = definition.Dice;
        _amount = definition.Amount;
        _armorPiercing = definition.ArmorPiercing;
        _halfOnSave = definition.HalfOnSave;
        _scalingStat = definition.ScalingStat;
        _scalingMultiplier = definition.ScalingMultiplier;

        // Parse damage type
        if (!string.IsNullOrEmpty(definition.DamageType) &&
            Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
        {
            _damageType = dt;
        }
        else
        {
            _damageType = DamageType.Bludgeoning;
        }
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return;
        }

        // Calculate base damage
        int damage = _amount;
        if (!string.IsNullOrEmpty(_dice))
        {
            damage += DiceRoller.Roll(_dice);
        }

        // Add stat scaling
        if (!string.IsNullOrEmpty(_scalingStat) && context.Caster != null)
        {
            int statValue = context.GetCasterStat(_scalingStat);
            damage += (int)(statValue * _scalingMultiplier);
        }

        // Ensure minimum damage
        damage = Math.Max(1, damage);

        // Apply half damage on save if configured
        if (_halfOnSave && state.SaveSucceeded)
        {
            damage /= 2;
        }

        // Skip if no damage to deal
        if (damage <= 0)
        {
            return;
        }

        // Apply damage with armor piercing
        int actualDamage = healthComponent.TakeDamage(
            damage,
            _damageType,
            context.Caster,
            applyArmor: true,
            armorPiercing: _armorPiercing
        );

        // Track damage dealt in state
        state.DamageDealt += actualDamage;

        // Mark effect as successful
        if (actualDamage > 0)
        {
            state.Success = true;
            messages.AddDamage(target, actualDamage, _damageType);
        }
    }
}
