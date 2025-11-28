using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that modifies the target's willpower.
/// Supports restoration (positive) and drain (negative) amounts.
/// </summary>
public class ModifyWillpowerStep : IEffectStep
{
    private readonly int _amount;
    private readonly string? _dice;
    private readonly string? _scalingStat;
    private readonly float _scalingMultiplier;

    public ModifyWillpowerStep(StepDefinition definition)
    {
        _amount = definition.Amount;
        _dice = definition.Dice;
        _scalingStat = definition.ScalingStat;
        _scalingMultiplier = definition.ScalingMultiplier;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var willpowerComponent = target.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        if (willpowerComponent == null)
        {
            messages.Add($"{target.DisplayName} has no Willpower.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Calculate amount with dice and scaling
        int amount = _amount;

        if (!string.IsNullOrEmpty(_dice))
        {
            amount += DiceRoller.Roll(_dice);
        }

        if (!string.IsNullOrEmpty(_scalingStat) && context.Caster != null)
        {
            int statValue = context.GetCasterStat(_scalingStat);
            amount += (int)(statValue * _scalingMultiplier);
        }

        if (amount == 0)
        {
            return;
        }

        if (amount > 0)
        {
            // Restoration
            int oldWP = willpowerComponent.CurrentWillpower;
            willpowerComponent.RestoreWillpower(amount);
            int actualRestored = willpowerComponent.CurrentWillpower - oldWP;

            if (actualRestored == 0)
            {
                messages.Add($"{target.DisplayName} is already at full Willpower.", Palette.ToHex(Palette.Disabled));
                return;
            }

            messages.Add($"{target.DisplayName} restores {actualRestored} Willpower.", Palette.ToHex(Palette.StatusBuff));
            state.Success = true;
        }
        else
        {
            // Drain (negative amount)
            int drainAmount = -amount;
            int oldWP = willpowerComponent.CurrentWillpower;
            willpowerComponent.SpendWillpower(drainAmount);
            int actualDrained = oldWP - willpowerComponent.CurrentWillpower;

            messages.Add($"{target.DisplayName} loses {actualDrained} Willpower.", Palette.ToHex(Palette.StatusDebuff));
            state.Success = true;
        }
    }
}
