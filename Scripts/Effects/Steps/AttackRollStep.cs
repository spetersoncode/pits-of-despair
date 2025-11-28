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
    private readonly bool _useMeleeModifier;

    public AttackRollStep(StepDefinition definition)
    {
        _attackStat = definition.AttackStat ?? "wil";
        _stopOnMiss = definition.StopOnMiss;
        _useMeleeModifier = definition.UseMeleeModifier;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Get attack modifier
        int attackMod;
        if (_useMeleeModifier && context.Caster != null)
        {
            var casterStats = context.Caster.GetNodeOrNull<StatsComponent>("StatsComponent");
            attackMod = casterStats?.GetAttackModifier(isMelee: true) ?? 0;
        }
        else
        {
            attackMod = context.GetCasterStat(_attackStat);
        }

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
