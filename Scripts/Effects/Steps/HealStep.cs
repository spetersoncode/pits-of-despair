using System;
using PitsOfDespair.Components;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that heals the target.
/// Supports dice, flat amounts, percentage of max HP, and stat scaling.
/// </summary>
public class HealStep : IEffectStep
{
    private readonly string? _dice;
    private readonly int _amount;
    private readonly bool _percent;
    private readonly string? _scalingStat;
    private readonly float _scalingMultiplier;

    public HealStep(StepDefinition definition)
    {
        _dice = definition.Dice;
        _amount = definition.Amount;
        _percent = definition.Percent;
        _scalingStat = definition.ScalingStat;
        _scalingMultiplier = definition.ScalingMultiplier;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return;
        }

        // Calculate healing amount
        int healing;
        if (_percent)
        {
            healing = (int)(healthComponent.MaxHealth * _amount / 100f);
        }
        else
        {
            healing = _amount;
            if (!string.IsNullOrEmpty(_dice))
            {
                healing += DiceRoller.Roll(_dice);
            }

            // Add stat scaling
            if (!string.IsNullOrEmpty(_scalingStat) && context.Caster != null)
            {
                int statValue = context.GetCasterStat(_scalingStat);
                healing += (int)(statValue * _scalingMultiplier);
            }
        }

        healing = Math.Max(0, healing);

        if (healing <= 0)
        {
            return;
        }

        // Apply healing
        int oldHealth = healthComponent.CurrentHealth;
        healthComponent.Heal(healing);
        int actualHealing = healthComponent.CurrentHealth - oldHealth;

        if (actualHealing > 0)
        {
            state.Success = true;
            messages.AddHeal(target, actualHealing);
        }
    }
}
