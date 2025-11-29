using System;
using Godot;
using PitsOfDespair.Conditions;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that applies a primed attack condition to the caster.
/// The prime adds bonuses to the next successful melee attack.
/// </summary>
public class ApplyPrimeStep : IEffectStep
{
    private readonly string _primeName;
    private readonly int _hitBonus;
    private readonly int _damageBonus;
    private readonly string _duration;
    private readonly PrimeTargetingMode _targetingMode;

    public ApplyPrimeStep(StepDefinition definition)
    {
        _primeName = definition.PrimeName ?? "Primed Attack";
        _hitBonus = definition.HitBonus;
        _damageBonus = definition.DamageBonus;
        _duration = definition.GetDurationString();
        _targetingMode = ParseTargetingMode(definition.TargetingMode);
    }

    private static PrimeTargetingMode ParseTargetingMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode))
            return PrimeTargetingMode.Single;

        return mode.ToLower() switch
        {
            "arc" => PrimeTargetingMode.Arc,
            "cleave" => PrimeTargetingMode.Arc,
            _ => PrimeTargetingMode.Single
        };
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Prime is applied to the caster, not the target
        var caster = context.Caster;
        if (caster == null)
        {
            GD.PrintErr("ApplyPrimeStep: No caster available");
            return;
        }

        // Create the primed attack condition
        var prime = new PrimedAttackCondition(_primeName, _duration)
        {
            HitBonus = _hitBonus,
            DamageBonus = _damageBonus,
            TargetingMode = _targetingMode
        };

        // Apply to caster (replaces any existing prime due to same TypeId)
        var conditionMessage = prime.OnApplied(caster);
        caster.AddConditionWithoutMessage(prime);

        // Add message
        if (!string.IsNullOrEmpty(conditionMessage.Message))
        {
            messages.AddConditionApplied(caster, conditionMessage.Message, conditionMessage.Color);
        }

        state.Success = true;
    }
}
