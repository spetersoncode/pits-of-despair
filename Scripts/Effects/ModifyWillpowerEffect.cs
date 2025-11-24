using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that modifies the target entity's willpower.
/// Supports both restoration (positive) and drain (negative) amounts.
/// Supports flat amounts, dice-based amounts, and stat scaling.
/// </summary>
public class ModifyWillpowerEffect : Effect
{
    public override string Type => "modify_willpower";
    public override string Name => "Modify Willpower";

    /// <summary>
    /// Base amount to modify willpower by. Positive restores, negative drains.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable amount (e.g., "1d6").
    /// Added to the base amount.
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale amount with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public ModifyWillpowerEffect() { }

    /// <summary>
    /// Creates a modify willpower effect with a fixed amount.
    /// Positive values restore, negative values drain.
    /// </summary>
    public ModifyWillpowerEffect(int amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Creates a modify willpower effect from a unified effect definition.
    /// </summary>
    public ModifyWillpowerEffect(EffectDefinition definition)
    {
        Amount = definition.Amount;
        Dice = definition.Dice;
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;
        var willpowerComponent = target.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        if (willpowerComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{targetName} has no Willpower.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate amount with dice and scaling
        int amount = CalculateScaledAmount(Amount, Dice, ScalingStat, ScalingMultiplier, context);

        if (amount == 0)
        {
            return EffectResult.CreateFailure(
                $"{context.GetSourceName()} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        if (amount > 0)
        {
            // Restoration
            int oldWP = willpowerComponent.CurrentWillpower;
            willpowerComponent.RestoreWillpower(amount);
            int actualRestored = willpowerComponent.CurrentWillpower - oldWP;

            if (actualRestored == 0)
            {
                return EffectResult.CreateFailure(
                    $"{targetName} is already at full Willpower.",
                    Palette.ToHex(Palette.Disabled)
                );
            }

            return EffectResult.CreateSuccess(
                $"{targetName} restores {actualRestored} Willpower.",
                Palette.ToHex(Palette.StatusBuff),
                target
            );
        }
        else
        {
            // Drain (negative amount)
            int drainAmount = -amount;
            int oldWP = willpowerComponent.CurrentWillpower;
            willpowerComponent.SpendWillpower(drainAmount);
            int actualDrained = oldWP - willpowerComponent.CurrentWillpower;

            return EffectResult.CreateSuccess(
                $"{targetName} loses {actualDrained} Willpower.",
                Palette.ToHex(Palette.StatusDebuff),
                target
            );
        }
    }
}
