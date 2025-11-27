using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI;

/// <summary>
/// Base class for goal-stack AI system.
/// Goals persist on a stack until completed, and can push sub-goals for complex operations.
/// </summary>
public abstract class Goal
{
    /// <summary>
    /// Reference to the goal that created this one.
    /// Used for failure recovery - when this goal fails,
    /// control returns to OriginalIntent for replanning.
    /// </summary>
    public Goal OriginalIntent { get; set; }

    /// <summary>
    /// The entity this goal belongs to.
    /// </summary>
    public BaseEntity Owner { get; protected set; }

    /// <summary>
    /// Returns true when this goal's objective is complete.
    /// Finished goals are automatically removed from the stack.
    /// </summary>
    public virtual bool IsFinished(AIContext context) => false;

    /// <summary>
    /// Executes one step toward completing this goal.
    /// May push sub-goals onto the stack for complex operations.
    /// </summary>
    public abstract void TakeAction(AIContext context);

    /// <summary>
    /// Called when this goal fails and cannot continue.
    /// Default behavior: pop goals until OriginalIntent, let it replan.
    /// </summary>
    public virtual void Fail(AIContext context)
    {
        context.AIComponent.GoalStack.FailToIntent(this);
    }

    /// <summary>
    /// Debug name for this goal.
    /// </summary>
    public virtual string GetName() => GetType().Name;
}
