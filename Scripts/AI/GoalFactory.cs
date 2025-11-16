using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI.Goals;

namespace PitsOfDespair.AI;

/// <summary>
/// Factory for creating Goal instances from string identifiers.
/// Used to instantiate goals based on creature data configuration.
/// </summary>
public static class GoalFactory
{
    private static readonly Dictionary<string, Func<Goal>> _goalRegistry = new()
    {
        { "MeleeAttack", () => new MeleeAttackGoal() },
        { "SearchLastKnown", () => new SearchLastKnownPositionGoal() },
        { "ReturnToSpawn", () => new ReturnToSpawnGoal() },
        { "Wander", () => new WanderGoal() },
        { "Idle", () => new IdleGoal() }
    };

    /// <summary>
    /// Creates a goal instance from a goal ID string.
    /// </summary>
    /// <param name="goalId">The goal identifier (e.g., "MeleeAttack", "Wander")</param>
    /// <returns>A new instance of the goal, or null if the ID is not recognized</returns>
    public static Goal CreateGoal(string goalId)
    {
        if (string.IsNullOrEmpty(goalId))
        {
            GD.PrintErr("GoalFactory: Cannot create goal from null or empty ID");
            return null;
        }

        if (_goalRegistry.TryGetValue(goalId, out var factory))
        {
            return factory();
        }

        GD.PrintErr($"GoalFactory: Unknown goal ID '{goalId}'");
        return null;
    }

    /// <summary>
    /// Creates multiple goal instances from a list of goal IDs.
    /// </summary>
    /// <param name="goalIds">List of goal identifiers</param>
    /// <returns>List of goal instances (skips invalid IDs)</returns>
    public static List<Goal> CreateGoals(List<string> goalIds)
    {
        var goals = new List<Goal>();

        if (goalIds == null || goalIds.Count == 0)
        {
            GD.PrintErr("GoalFactory: No goal IDs provided");
            return goals;
        }

        foreach (var goalId in goalIds)
        {
            var goal = CreateGoal(goalId);
            if (goal != null)
            {
                goals.Add(goal);
            }
        }

        return goals;
    }

    /// <summary>
    /// Gets all registered goal IDs.
    /// </summary>
    public static IEnumerable<string> GetRegisteredGoalIds() => _goalRegistry.Keys;
}
