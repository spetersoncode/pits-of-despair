using System;
using System.Linq;
using Godot;
using PitsOfDespair.Conditions;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that applies a prepared attack condition to the caster.
/// The prepared attack adds bonuses to the next successful melee attack.
/// </summary>
public class ApplyPrepareStep : IEffectStep
{
    private readonly string _prepareName;
    private readonly int _hitBonus;
    private readonly int _damageBonus;
    private readonly string _duration;
    private readonly PrepareTargetingMode _targetingMode;

    public ApplyPrepareStep(StepDefinition definition)
    {
        _prepareName = definition.PrepareName ?? "Prepared Attack";
        _hitBonus = definition.HitBonus;
        _damageBonus = definition.DamageBonus;
        _duration = definition.GetDurationString();
        _targetingMode = ParseTargetingMode(definition.TargetingMode);
    }

    private static PrepareTargetingMode ParseTargetingMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode))
            return PrepareTargetingMode.Single;

        return mode.ToLower() switch
        {
            "arc" => PrepareTargetingMode.Arc,
            "cleave" => PrepareTargetingMode.Arc,
            _ => PrepareTargetingMode.Single
        };
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Prepared attack is applied to the caster, not the target
        var caster = context.Caster;
        if (caster == null)
        {
            GD.PrintErr("ApplyPrepareStep: No caster available");
            return;
        }

        // Check for existing prepared attack
        var existingPrepare = caster.GetActiveConditions()
            .FirstOrDefault(c => c.TypeId == "prepared_attack") as PreparedAttackCondition;

        if (existingPrepare != null && existingPrepare.Name == _prepareName)
        {
            // Same prepared attack already active - prevent with message
            messages.Add($"Already preparing {_prepareName}!", Core.Palette.ToHex(Core.Palette.Caution));
            state.Success = false;
            return;
        }

        // Create the prepared attack condition
        var prepare = new PreparedAttackCondition(_prepareName, _duration)
        {
            HitBonus = _hitBonus,
            DamageBonus = _damageBonus,
            TargetingMode = _targetingMode
        };

        // Apply to caster (replaces any existing different prepared attack due to same TypeId)
        var conditionMessage = prepare.OnApplied(caster);
        caster.AddConditionWithoutMessage(prepare);

        // Add message
        if (!string.IsNullOrEmpty(conditionMessage.Message))
        {
            messages.AddConditionApplied(caster, conditionMessage.Message, conditionMessage.Color);
        }

        state.Success = true;
    }
}
