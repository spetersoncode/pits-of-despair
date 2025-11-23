using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that restores Willpower to the target.
/// Supports flat restoration, dice-based restoration, and stat scaling.
/// </summary>
public class RestoreWillpowerEffect : Effect
{
    public override string Type => "restore_willpower";
    public override string Name => "Restore Willpower";

    /// <summary>
    /// Flat WP restoration amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Dice notation for variable restoration (e.g., "1d6").
    /// </summary>
    public string? Dice { get; set; }

    /// <summary>
    /// Stat to scale restoration with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    public RestoreWillpowerEffect() { }

    /// <summary>
    /// Creates a restore WP effect with a fixed amount.
    /// </summary>
    public RestoreWillpowerEffect(int amount)
    {
        Amount = amount;
    }

    /// <summary>
    /// Creates a restore WP effect from a unified effect definition.
    /// </summary>
    public RestoreWillpowerEffect(EffectDefinition definition)
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

        // Check if already at full WP
        if (willpowerComponent.CurrentWillpower >= willpowerComponent.MaxWillpower)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is already at full Willpower.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate restoration amount with dice and scaling
        int restoration = CalculateScaledAmount(Amount, Dice, ScalingStat, ScalingMultiplier, context);

        if (restoration <= 0)
        {
            return EffectResult.CreateFailure(
                $"{context.GetSourceName()} has no effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Apply restoration
        int oldWP = willpowerComponent.CurrentWillpower;
        willpowerComponent.RestoreWillpower(restoration);
        int actualRestored = willpowerComponent.CurrentWillpower - oldWP;

        return EffectResult.CreateSuccess(
            $"{targetName} restores {actualRestored} WP.",
            Palette.ToHex(Palette.StatusBuff),
            target
        );
    }
}
