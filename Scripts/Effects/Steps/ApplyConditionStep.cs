using Godot;
using PitsOfDespair.Conditions;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that applies a condition to the target.
/// Supports conditional application based on save failure or damage dealt.
/// </summary>
public class ApplyConditionStep : IEffectStep
{
    private readonly string? _conditionType;
    private readonly int _amount;
    private readonly string _duration;
    private readonly string? _dotDamage;
    private readonly bool _requireSaveFailed;
    private readonly bool _requireDamageDealt;
    private readonly int _armorPiercing;

    public ApplyConditionStep(StepDefinition definition)
    {
        _conditionType = definition.ConditionType;
        _amount = definition.Amount;
        _duration = definition.GetDurationString();
        _dotDamage = definition.DotDamage;
        _requireSaveFailed = definition.RequireSaveFailed;
        _requireDamageDealt = definition.RequireDamageDealt;
        _armorPiercing = definition.ArmorPiercing;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Check preconditions
        if (_requireSaveFailed && !state.SaveFailed)
        {
            return;
        }

        if (_requireDamageDealt && state.DamageDealt <= 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(_conditionType))
        {
            GD.PrintErr("ApplyConditionStep: No condition type specified");
            return;
        }

        // Create condition using the factory
        var condition = ConditionFactory.Create(_conditionType, _amount, _duration);
        if (condition == null)
        {
            GD.PrintErr($"ApplyConditionStep: Unknown condition type '{_conditionType}'");
            return;
        }

        // Special handling for AcidCondition - set DoT damage and armor piercing
        if (condition is AcidCondition acidCondition)
        {
            acidCondition.Source = context.Caster;
            acidCondition.ArmorPiercing = _armorPiercing;

            // If DoT damage is specified, create a new condition with the right damage
            if (!string.IsNullOrEmpty(_dotDamage))
            {
                condition = new AcidCondition(_duration, _dotDamage)
                {
                    Source = context.Caster,
                    ArmorPiercing = _armorPiercing
                };
            }
        }

        // Apply condition to target
        var conditionMessage = condition.OnApplied(context.Target);
        context.Target.AddConditionWithoutMessage(condition);

        // Add condition message
        if (!string.IsNullOrEmpty(conditionMessage.Message))
        {
            messages.AddConditionApplied(conditionMessage.Message, conditionMessage.Color);
        }

        state.Success = true;
    }
}
