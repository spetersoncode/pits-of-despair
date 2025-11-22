using PitsOfDespair.Actions;

namespace PitsOfDespair.AI;

/// <summary>
/// Wrapper that adapts an Action for AI use.
/// Holds the action, weight for random selection, and debug info.
/// Provides consistent execution that returns ActionResult.
/// </summary>
public class AIAction
{
    /// <summary>
    /// The underlying Action to execute.
    /// </summary>
    public Action Action { get; }

    /// <summary>
    /// Weight for random selection. Higher = more likely to be chosen.
    /// </summary>
    public int Weight { get; }

    /// <summary>
    /// Debug name for logging and inspection.
    /// </summary>
    public string DebugName { get; }

    public AIAction(Action action, int weight, string debugName = null)
    {
        Action = action;
        Weight = weight;
        DebugName = debugName ?? action?.Name ?? "Unknown";
    }

    /// <summary>
    /// Executes the action and returns the result.
    /// </summary>
    public ActionResult Execute(AIContext context)
    {
        if (Action == null)
        {
            return ActionResult.CreateFailure("No action to execute");
        }

        return context.Entity.ExecuteAction(Action, context.ActionContext);
    }
}
