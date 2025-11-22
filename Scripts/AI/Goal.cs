using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI;

/// <summary>
/// Base class for goal-stack AI system.
/// Goals persist on a stack until completed, and can push sub-goals for complex operations.
///
/// This class supports both the new stack-based system (IsFinished, TakeAction) and
/// the legacy utility-scoring system (CalculateScore, Execute) during the transition period.
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

    #region New Stack-Based System

    /// <summary>
    /// Returns true when this goal's objective is complete.
    /// Finished goals are automatically removed from the stack.
    /// </summary>
    public virtual bool IsFinished(AIContext context) => false;

    /// <summary>
    /// Executes one step toward completing this goal.
    /// May push sub-goals onto the stack for complex operations.
    /// </summary>
    public virtual void TakeAction(AIContext context)
    {
        // Default: delegate to legacy Execute method for backwards compatibility
        Execute(context);
    }

    /// <summary>
    /// Called when this goal fails and cannot continue.
    /// Default behavior: pop goals until OriginalIntent, let it replan.
    /// </summary>
    public virtual void Fail(AIContext context)
    {
        context.AIComponent.GoalStack.FailToIntent(this);
    }

    #endregion

    #region Legacy Utility-Scoring System (Transition Period)

    /// <summary>
    /// [LEGACY] Calculates a utility score for this goal.
    /// Override this for goals using the old utility-scoring system.
    /// </summary>
    public virtual float CalculateScore(AIContext context) => 0f;

    /// <summary>
    /// [LEGACY] Executes the goal's action.
    /// Override this for goals using the old utility-scoring system.
    /// </summary>
    public virtual ActionResult Execute(AIContext context) => ActionResult.CreateFailure("Not implemented");

    /// <summary>
    /// [LEGACY] Called when this goal becomes the active goal.
    /// </summary>
    public virtual void OnActivated(AIContext context) { }

    /// <summary>
    /// [LEGACY] Called when this goal is no longer the active goal.
    /// </summary>
    public virtual void OnDeactivated(AIContext context) { }

    #endregion

    /// <summary>
    /// Debug name for this goal.
    /// </summary>
    public virtual string GetName() => GetType().Name;
}
