using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that performs an opposed saving throw.
/// Sets SaveSucceeded/SaveFailed in state and optionally stops pipeline.
/// Supports both single AttackStat and multiple AttackStats (averaged for hybrid skills).
/// </summary>
public class SaveCheckStep : IEffectStep
{
    private readonly string? _saveStat;
    private readonly string? _attackStat;
    private readonly List<string>? _attackStats;
    private readonly int _saveModifier;
    private readonly bool _stopOnSuccess;
    private readonly bool _halfOnSuccess;

    public SaveCheckStep(StepDefinition definition)
    {
        _saveStat = definition.SaveStat;
        _attackStat = definition.AttackStat;
        _attackStats = definition.AttackStats;
        _saveModifier = definition.SaveModifier;
        _stopOnSuccess = definition.StopOnSuccess;
        _halfOnSuccess = definition.HalfOnSuccess;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // No save stat = automatic success for effect
        if (string.IsNullOrEmpty(_saveStat))
        {
            state.SaveFailed = true;
            return;
        }

        // Perform the save check silently (MessageCollector handles messaging)
        // AttackStats (averaged) takes precedence over single AttackStat
        bool resisted = _attackStats is { Count: > 0 }
            ? SavingThrow.TryResistSilent(context, _saveStat, _attackStats, _saveModifier)
            : SavingThrow.TryResistSilent(context, _saveStat, _attackStat, _saveModifier);

        if (resisted)
        {
            state.SaveSucceeded = true;
            state.SaveFailed = false;

            // Emit resistance message
            messages.AddSaveResist(context.Target);

            // Stop pipeline if configured
            if (_stopOnSuccess)
            {
                state.Continue = false;
            }
        }
        else
        {
            state.SaveSucceeded = false;
            state.SaveFailed = true;
        }
    }
}
