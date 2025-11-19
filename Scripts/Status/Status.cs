using PitsOfDespair.Actions;
using PitsOfDespair.Entities;
using PitsOfDespair.Core;

namespace PitsOfDespair.Status;

/// <summary>
/// Types of stats that can be buffed by status effects.
/// </summary>
public enum StatType
{
    Armor,
    Strength,
    Agility,
    Endurance
}

/// <summary>
/// Represents a status message with associated color.
/// </summary>
public readonly struct StatusMessage
{
    public string Message { get; init; }
    public string Color { get; init; }

    public StatusMessage(string message, string color)
    {
        Message = message;
        Color = color;
    }

    public static StatusMessage Empty => new StatusMessage(string.Empty, Palette.ToHex(Palette.Default));
}

/// <summary>
/// Base class for all status effects in the game.
/// Statuses are temporary conditions that persist for a duration (buffs, debuffs, etc.).
/// </summary>
public abstract class Status
{
    /// <summary>
    /// The name of this status type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The type identifier for this status (used for non-stacking logic).
    /// Statuses with the same TypeId will not stack.
    /// </summary>
    public abstract string TypeId { get; }

    /// <summary>
    /// Total duration of this status in turns.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Remaining turns before this status expires.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Called when this status is first applied to an entity.
    /// Use this to apply stat modifiers or other initial effects.
    /// Returns a status message with color describing the effect application.
    /// </summary>
    /// <param name="target">The entity receiving the status.</param>
    /// <returns>StatusMessage with text and color, or StatusMessage.Empty for no message.</returns>
    public abstract StatusMessage OnApplied(BaseEntity target);

    /// <summary>
    /// Called when this status is removed from an entity (either by expiration or manual removal).
    /// Use this to clean up stat modifiers or other effects.
    /// Returns a status message with color describing the effect removal.
    /// </summary>
    /// <param name="target">The entity losing the status.</param>
    /// <returns>StatusMessage with text and color, or StatusMessage.Empty for no message.</returns>
    public abstract StatusMessage OnRemoved(BaseEntity target);

    /// <summary>
    /// Called each turn while this status is active.
    /// Override this if the status needs to do something each turn (e.g., poison damage).
    /// Returns a status message with color describing turn effects.
    /// </summary>
    /// <param name="target">The entity with this status.</param>
    /// <returns>StatusMessage with text and color, or StatusMessage.Empty for no message.</returns>
    public virtual StatusMessage OnTurnProcessed(BaseEntity target)
    {
        // Default: do nothing each turn
        return StatusMessage.Empty;
    }

    /// <summary>
    /// Refreshes the duration of this status.
    /// Used when the same status type is applied again.
    /// </summary>
    /// <param name="newDuration">The new duration to set.</param>
    public void RefreshDuration(int newDuration)
    {
        if (newDuration > RemainingTurns)
        {
            RemainingTurns = newDuration;
            Duration = newDuration;
        }
    }
}
