using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Status;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies a status (buff/debuff) to the target entity.
/// </summary>
public class ApplyStatusEffect : Effect
{
    /// <summary>
    /// The type of status to apply (e.g., "armor_buff", "poison", "paralyze", "confusion").
    /// </summary>
    public string StatusType { get; set; }

    /// <summary>
    /// The amount/magnitude of the status effect.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration of the status in turns.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Dice notation for duration (e.g., "2d3"). Overrides Duration if specified.
    /// </summary>
    public string DurationDice { get; set; }

    public override string Name => "Apply Status";

    public ApplyStatusEffect()
    {
        StatusType = string.Empty;
        Amount = 0;
        Duration = 0;
        DurationDice = string.Empty;
    }

    public ApplyStatusEffect(string statusType, int amount, int duration, string durationDice = "")
    {
        StatusType = statusType;
        Amount = amount;
        Duration = duration;
        DurationDice = durationDice;
    }

    public override EffectResult Apply(BaseEntity target, ActionContext context)
    {
        var name = target.DisplayName;
        var statusComponent = target.GetNodeOrNull<StatusComponent>("StatusComponent");

        if (statusComponent == null)
        {
            return new EffectResult(
                false,
                $"{name} cannot receive status effects.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Determine actual duration (roll dice if specified, otherwise use Duration)
        int actualDuration = Duration;
        if (!string.IsNullOrEmpty(DurationDice))
        {
            actualDuration = DiceRoller.Roll(DurationDice);
        }

        // Create the appropriate status based on type
        var status = CreateStatus(StatusType, Amount, actualDuration);
        if (status == null)
        {
            GD.PrintErr($"ApplyStatusEffect: Unknown status type '{StatusType}'");
            return new EffectResult(
                false,
                $"Failed to apply status effect.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Add status to target (message will be emitted via signal)
        statusComponent.AddStatus(status);

        // Return success (the actual message is emitted by StatusComponent signal)
        return new EffectResult(
            true,
            string.Empty,
            Palette.ToHex(Palette.Success)
        );
    }

    /// <summary>
    /// Factory method to create Status instances from type string.
    /// </summary>
    private Status.Status? CreateStatus(string statusType, int amount, int duration)
    {
        switch (statusType.ToLower())
        {
            case "armor_buff":
                return new ArmorBuffStatus(amount, duration);

            case "strength_buff":
                return new StrengthBuffStatus(amount, duration);

            case "agility_buff":
                return new AgilityBuffStatus(amount, duration);

            case "endurance_buff":
                return new EnduranceBuffStatus(amount, duration);

            case "confusion":
                return new ConfusionStatus(duration);

            default:
                return null;
        }
    }
}
