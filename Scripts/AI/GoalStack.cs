using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.AI;

/// <summary>
/// Stack data structure for managing AI goals.
/// Goals persist until completed, with the top goal executing each turn.
/// </summary>
public class GoalStack
{
    private Stack<Goal> _stack = new Stack<Goal>();

    public int Count => _stack.Count;
    public bool IsEmpty => _stack.Count == 0;

    public void Push(Goal goal)
    {
        _stack.Push(goal);
    }

    public Goal Pop()
    {
        return _stack.Count > 0 ? _stack.Pop() : null;
    }

    public Goal Peek()
    {
        return _stack.Count > 0 ? _stack.Peek() : null;
    }

    /// <summary>
    /// Removes all goals where IsFinished() returns true.
    /// Called at the start of each turn.
    /// </summary>
    public void RemoveFinished(AIContext context)
    {
        // Rebuild stack without finished goals
        var remaining = new Stack<Goal>();
        foreach (var goal in _stack.Reverse())
        {
            if (!goal.IsFinished(context))
            {
                remaining.Push(goal);
            }
        }
        _stack = remaining;
    }

    /// <summary>
    /// Pops goals until we reach the failed goal's OriginalIntent.
    /// The OriginalIntent goal can then replan with updated world state.
    /// </summary>
    public void FailToIntent(Goal failedGoal)
    {
        while (_stack.Count > 0)
        {
            Goal top = _stack.Pop();
            if (top == failedGoal.OriginalIntent)
            {
                _stack.Push(top); // Keep the intent goal
                break;
            }
        }
    }

    /// <summary>
    /// Clears all goals from the stack.
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
    }

    /// <summary>
    /// Returns stack contents for debugging (bottom to top).
    /// </summary>
    public IEnumerable<Goal> GetAllGoals() => _stack.Reverse();
}
