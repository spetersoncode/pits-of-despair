using PitsOfDespair.Actions;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

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
    /// Returns a message describing the effect application (empty string for no message).
    /// </summary>
    /// <param name="target">The entity receiving the status.</param>
    /// <returns>Message to display, or empty string for no message.</returns>
    public abstract string OnApplied(BaseEntity target);

    /// <summary>
    /// Called when this status is removed from an entity (either by expiration or manual removal).
    /// Use this to clean up stat modifiers or other effects.
    /// Returns a message describing the effect removal (empty string for no message).
    /// </summary>
    /// <param name="target">The entity losing the status.</param>
    /// <returns>Message to display, or empty string for no message.</returns>
    public abstract string OnRemoved(BaseEntity target);

    /// <summary>
    /// Called each turn while this status is active.
    /// Override this if the status needs to do something each turn (e.g., poison damage).
    /// Returns a message describing turn effects (empty string for no message).
    /// </summary>
    /// <param name="target">The entity with this status.</param>
    /// <returns>Message to display, or empty string for no message.</returns>
    public virtual string OnTurnProcessed(BaseEntity target)
    {
        // Default: do nothing each turn
        return string.Empty;
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
