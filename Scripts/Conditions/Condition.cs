using PitsOfDespair.Entities;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Types of stats that can be buffed by conditions.
/// </summary>
public enum StatType
{
    Armor,
    Strength,
    Agility,
    Endurance
}

/// <summary>
/// Represents a condition message with associated color.
/// </summary>
public readonly struct ConditionMessage
{
    public string Message { get; init; }
    public string Color { get; init; }

    public ConditionMessage(string message, string color)
    {
        Message = message;
        Color = color;
    }

    public static ConditionMessage Empty => new ConditionMessage(string.Empty, Palette.ToHex(Palette.Default));
}

/// <summary>
/// Base class for all conditions in the game.
/// Conditions are temporary effects that persist for a duration (buffs, debuffs, etc.).
/// </summary>
public abstract class Condition
{
    /// <summary>
    /// The name of this condition type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The type identifier for this condition (used for non-stacking logic).
    /// Conditions with the same TypeId will not stack.
    /// </summary>
    public abstract string TypeId { get; }

    /// <summary>
    /// Duration of this condition as dice notation (e.g., "10", "2d3", "1d4+2").
    /// Resolved via DiceRoller when the condition is applied.
    /// </summary>
    public string Duration { get; set; } = "1";

    /// <summary>
    /// Remaining turns before this condition expires.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Resolves the duration by rolling dice notation.
    /// Called by ConditionComponent when adding this condition.
    /// </summary>
    public int ResolveDuration()
    {
        return DiceRoller.Roll(Duration);
    }

    /// <summary>
    /// Called when this condition is first applied to an entity.
    /// Use this to apply stat modifiers or other initial effects.
    /// Returns a condition message with color describing the effect application.
    /// </summary>
    /// <param name="target">The entity receiving the condition.</param>
    /// <returns>ConditionMessage with text and color, or ConditionMessage.Empty for no message.</returns>
    public abstract ConditionMessage OnApplied(BaseEntity target);

    /// <summary>
    /// Called when this condition is removed from an entity (either by expiration or manual removal).
    /// Use this to clean up stat modifiers or other effects.
    /// Returns a condition message with color describing the effect removal.
    /// </summary>
    /// <param name="target">The entity losing the condition.</param>
    /// <returns>ConditionMessage with text and color, or ConditionMessage.Empty for no message.</returns>
    public abstract ConditionMessage OnRemoved(BaseEntity target);

    /// <summary>
    /// Called each turn while this condition is active.
    /// Override this if the condition needs to do something each turn (e.g., poison damage).
    /// Returns a condition message with color describing turn effects.
    /// </summary>
    /// <param name="target">The entity with this condition.</param>
    /// <returns>ConditionMessage with text and color, or ConditionMessage.Empty for no message.</returns>
    public virtual ConditionMessage OnTurnProcessed(BaseEntity target)
    {
        // Default: do nothing each turn
        return ConditionMessage.Empty;
    }

    /// <summary>
    /// Refreshes the remaining turns of this condition.
    /// Used when the same condition type is applied again.
    /// Only extends if the new duration is longer than remaining.
    /// </summary>
    /// <param name="newDuration">The resolved duration to compare/set.</param>
    public void RefreshDuration(int newDuration)
    {
        if (newDuration > RemainingTurns)
        {
            RemainingTurns = newDuration;
        }
    }
}
