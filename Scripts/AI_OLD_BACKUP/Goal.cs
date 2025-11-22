using Godot;
using PitsOfDespair.Actions;

namespace PitsOfDespair.AI;

/// <summary>
/// Base class for all AI goals. Goals use utility-based scoring to determine
/// which action an AI entity should take on their turn.
/// </summary>
public abstract class Goal
{
    /// <summary>
    /// Calculates how desirable this goal is in the current context.
    /// </summary>
    /// <param name="context">Current AI context with entity state and environment info</param>
    /// <returns>Score where 0 = invalid/unwanted, higher values = more desirable (typically 0-100 range)</returns>
    public abstract float CalculateScore(AIContext context);

    /// <summary>
    /// Executes this goal's action.
    /// </summary>
    /// <param name="context">Current AI context with entity state and environment info</param>
    /// <returns>Result of the action execution</returns>
    public abstract ActionResult Execute(AIContext context);

    /// <summary>
    /// Called when this goal becomes the active goal (optional override).
    /// Useful for initialization or state tracking.
    /// </summary>
    public virtual void OnActivated(AIContext context) { }

    /// <summary>
    /// Called when this goal stops being the active goal (optional override).
    /// Useful for cleanup or state tracking.
    /// </summary>
    public virtual void OnDeactivated(AIContext context) { }

    /// <summary>
    /// Gets the name of this goal for debugging purposes.
    /// </summary>
    public virtual string GetName() => GetType().Name;
}
