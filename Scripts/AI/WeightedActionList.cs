using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PitsOfDespair.AI;

/// <summary>
/// Utility class for weighted random selection of AI actions.
/// Components add actions with weights; higher weight = more likely selection.
/// </summary>
public class WeightedActionList
{
    private List<WeightedAction> _actions = new List<WeightedAction>();

    private struct WeightedAction
    {
        public int Weight;
        public Action<AIContext> Execute;
        public string DebugName;
    }

    public bool IsEmpty => _actions.Count == 0;
    public int Count => _actions.Count;
    public int TotalWeight => _actions.Sum(a => a.Weight);

    /// <summary>
    /// Adds an action with the specified weight.
    /// Weight represents "number of balls in the bag" - higher = more likely.
    /// Most actions should use weight 1 for equal probability.
    /// </summary>
    public void Add(int weight, Action<AIContext> execute, string debugName = null)
    {
        if (weight <= 0) return;

        _actions.Add(new WeightedAction
        {
            Weight = weight,
            Execute = execute,
            DebugName = debugName ?? "Unknown"
        });
    }

    /// <summary>
    /// Picks a random action weighted by the weights.
    /// Returns null if list is empty.
    /// </summary>
    public Action<AIContext> PickRandomWeighted(RandomNumberGenerator rng = null)
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
                return action.Execute;
            }
        }

        return _actions[^1].Execute; // Fallback
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
