using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PitsOfDespair.AI;

/// <summary>
/// Utility class for weighted random selection of AI actions.
/// Components add AIAction objects; higher weight = more likely selection.
/// </summary>
public class WeightedActionList
{
    private List<AIAction> _actions = new List<AIAction>();

    public bool IsEmpty => _actions.Count == 0;
    public int Count => _actions.Count;
    public int TotalWeight => _actions.Sum(a => a.Weight);

    /// <summary>
    /// Adds an AIAction to the list.
    /// </summary>
    public void Add(AIAction action)
    {
        if (action == null || action.Weight <= 0)
            return;

        _actions.Add(action);
    }

    /// <summary>
    /// Picks a random action weighted by the weights.
    /// Returns null if list is empty.
    /// </summary>
    public AIAction PickRandomWeighted(RandomNumberGenerator rng = null)
    {
        if (_actions.Count == 0) return null;

        int total = TotalWeight;
        if (total <= 0) return null;

        rng ??= new RandomNumberGenerator();
        rng.Randomize();
        int roll = rng.RandiRange(0, total - 1);

        int cumulative = 0;
        foreach (var action in _actions)
        {
            cumulative += action.Weight;
            if (roll < cumulative)
            {
                return action;
            }
        }

        return _actions[^1]; // Fallback
    }

    /// <summary>
    /// Returns action names and weights for debugging.
    /// </summary>
    public IEnumerable<(string Name, int Weight)> GetDebugInfo()
    {
        return _actions.Select(a => (a.DebugName, a.Weight));
    }

    /// <summary>
    /// Clears all actions from the list.
    /// </summary>
    public void Clear()
    {
        _actions.Clear();
    }
}
