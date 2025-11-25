using Godot;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies a condition (buff/debuff) to the target entity.
/// Supports stat scaling for amount calculation.
/// </summary>
public class ApplyConditionEffect : Effect
{
    public override string Type => "apply_condition";
    public override string Name => "Apply Condition";

    /// <summary>
    /// The type of condition to apply (e.g., "armor_buff", "poison", "paralyze", "confusion").
    /// </summary>
    public string? ConditionType { get; set; }

    /// <summary>
    /// The amount/magnitude of the condition effect.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration as dice notation (e.g., "10", "2d3").
    /// </summary>
    public string Duration { get; set; }

    /// <summary>
    /// Stat to scale amount with.
    /// </summary>
    public string? ScalingStat { get; set; }

    /// <summary>
    /// Multiplier for stat scaling.
    /// </summary>
    public float ScalingMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Target's save stat for saving throw (e.g., "end", "wil").
    /// If set, target can resist the condition.
    /// </summary>
    private readonly string? _saveStat;

    /// <summary>
    /// Caster's attack stat for the opposed roll. Defaults to "wil".
    /// </summary>
    private readonly string? _attackStat;

    /// <summary>
    /// Modifier to caster's roll. Positive = harder to resist, negative = easier.
    /// </summary>
    private readonly int _saveModifier;

    public ApplyConditionEffect()
    {
        ConditionType = string.Empty;
        Amount = 0;
        Duration = "1";
    }

    public ApplyConditionEffect(string conditionType, int amount, string duration)
    {
        ConditionType = conditionType;
        Amount = amount;
        Duration = duration;
    }

    /// <summary>
    /// Creates an apply condition effect from a unified effect definition.
    /// </summary>
    public ApplyConditionEffect(EffectDefinition definition)
    {
        ConditionType = definition.ConditionType;
        Amount = definition.Amount;
        Duration = definition.GetDurationString();
        ScalingStat = definition.ScalingStat;
        ScalingMultiplier = definition.ScalingMultiplier;
        _saveStat = definition.SaveStat;
        _attackStat = definition.AttackStat;
        _saveModifier = definition.SaveModifier;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;

        // Check saving throw if configured (message emitted by SavingThrow)
        if (SavingThrow.TryResist(context, _saveStat, _attackStat, _saveModifier))
        {
            return EffectResult.CreateFailure(string.Empty, Palette.ToHex(Palette.Default));
        }

        // Calculate amount with scaling
        int finalAmount = CalculateScaledAmount(Amount, null, ScalingStat, ScalingMultiplier, context);

        // Create condition using the factory
        var condition = ConditionFactory.Create(ConditionType, finalAmount, Duration);
        if (condition == null)
        {
            GD.PrintErr($"ApplyConditionEffect: Unknown condition type '{ConditionType}'");
            return EffectResult.CreateFailure(
                $"Failed to apply condition.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Add condition to target (conditions are now managed by BaseEntity directly)
        // Note: AddCondition emits ConditionMessage signal, but that's only connected for the player.
        // We need to capture the message and emit it via CombatSystem for all entities.
        var conditionMessage = condition.OnApplied(target);
        target.AddConditionWithoutMessage(condition);

        // Emit condition applied message via CombatSystem so it shows for all entities
        if (!string.IsNullOrEmpty(conditionMessage.Message))
        {
            context.ActionContext.CombatSystem.EmitActionMessage(
                target,
                conditionMessage.Message,
                conditionMessage.Color
            );
        }

        return EffectResult.CreateSuccess(
            string.Empty,
            Palette.ToHex(Palette.StatusBuff),
            target
        );
    }
}
