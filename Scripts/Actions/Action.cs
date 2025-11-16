using PitsOfDespair.Entities;

namespace PitsOfDespair.Actions;

/// <summary>
/// Base class for all turn-consuming actions in the game.
/// Actions represent discrete activities that can be performed by entities (player or AI).
/// </summary>
public abstract class Action
{
    /// <summary>
    /// The display name of this action.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Validates whether this action can be executed by the given actor in the current game state.
    /// </summary>
    /// <param name="actor">The entity attempting to perform the action.</param>
    /// <param name="context">The action context containing game systems and state.</param>
    /// <returns>True if the action can be executed, false otherwise.</returns>
    public abstract bool CanExecute(BaseEntity actor, ActionContext context);

    /// <summary>
    /// Executes this action for the given actor.
    /// </summary>
    /// <param name="actor">The entity performing the action.</param>
    /// <param name="context">The action context containing game systems and state.</param>
    /// <returns>An ActionResult describing the outcome of the action.</returns>
    public abstract ActionResult Execute(BaseEntity actor, ActionContext context);
}
