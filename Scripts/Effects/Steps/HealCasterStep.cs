using System;
using PitsOfDespair.Components;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that heals the caster based on damage dealt.
/// Used for vampiric/life drain effects.
/// </summary>
public class HealCasterStep : IEffectStep
{
    private readonly float _fraction;

    public HealCasterStep(StepDefinition definition)
    {
        _fraction = definition.Fraction > 0 ? definition.Fraction : 0.5f;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Need damage to have been dealt
        if (state.DamageDealt <= 0)
        {
            return;
        }

        // Need a caster
        if (context.Caster == null)
        {
            return;
        }

        var casterHealth = context.Caster.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (casterHealth == null)
        {
            return;
        }

        // Calculate healing as fraction of damage dealt
        int healing = (int)(state.DamageDealt * _fraction);
        healing = Math.Max(0, healing);

        if (healing <= 0)
        {
            return;
        }

        // Apply healing to caster
        int oldHealth = casterHealth.CurrentHealth;
        casterHealth.Heal(healing);
        int actualHealing = casterHealth.CurrentHealth - oldHealth;

        if (actualHealing > 0)
        {
            state.Success = true;
            messages.AddHeal(context.Caster, actualHealing);
        }
    }
}
