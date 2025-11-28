using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that performs an opposed attack roll (attacker wins ties).
/// Sets AttackHit/AttackMissed in state and optionally stops pipeline on miss.
/// </summary>
public class AttackRollStep : IEffectStep
{
    private readonly string? _attackStat;
    private readonly bool _stopOnMiss;

    public AttackRollStep(StepDefinition definition)
    {
        _attackStat = definition.AttackStat ?? "wil";
        _stopOnMiss = definition.StopOnMiss;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Get attack modifier from caster stat
        int attackMod = context.GetCasterStat(_attackStat);

        // Get defense modifier from target
        var targetStats = context.Target.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Targets without stats (decorations/objects) have -10 defense - essentially auto-hit
        int defenseMod = targetStats?.GetDefenseModifier() ?? -10;

        // Opposed roll: 2d6 + attack vs 2d6 + defense
        // Attacker wins ties (unlike saves where defender wins)
        int attackRoll = DiceRoller.Roll(2, 6, attackMod);
        int defenseRoll = DiceRoller.Roll(2, 6, defenseMod);
        bool hit = attackRoll >= defenseRoll;

        if (hit)
        {
            state.AttackHit = true;
            state.AttackMissed = false;
        }
        else
        {
            state.AttackHit = false;
            state.AttackMissed = true;

            // Emit miss message
            messages.AddMiss(context.Target);

            // Stop pipeline if configured
            if (_stopOnMiss)
            {
                state.Continue = false;
            }
        }
    }
}
