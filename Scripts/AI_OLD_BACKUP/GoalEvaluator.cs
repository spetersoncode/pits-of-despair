using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PitsOfDespair.AI;

/// <summary>
/// Evaluates all available goals and selects the highest-scoring one.
/// </summary>
public static class GoalEvaluator
{
    /// <summary>
    /// Evaluates all goals and returns the one with the highest score.
    /// </summary>
    /// <param name="goals">List of available goals to evaluate</param>
    /// <param name="context">Current AI context</param>
    /// <returns>The highest-scoring goal, or null if no goals have a score > 0</returns>
    public static Goal EvaluateBestGoal(List<Goal> goals, AIContext context)
    {
        if (goals == null || goals.Count == 0)
        {
            GD.PrintErr("GoalEvaluator: No goals available to evaluate");
            return null;
        }

        Goal bestGoal = null;
        float bestScore = 0f;

        foreach (var goal in goals)
        {
            float score = goal.CalculateScore(context);

            // Goals with score <= 0 are invalid or unwanted
            if (score > bestScore)
            {
                bestScore = score;
                bestGoal = goal;
            }
        }

        if (bestGoal == null)
        {
            GD.PrintErr($"GoalEvaluator: No valid goals found for entity {context.Entity.DisplayName}");
        }

        return bestGoal;
    }

    /// <summary>
    /// Evaluates all goals and returns them sorted by score (for debugging).
    /// </summary>
    /// <param name="goals">List of available goals to evaluate</param>
    /// <param name="context">Current AI context</param>
    /// <returns>Goals sorted by score (highest first)</returns>
    public static List<(Goal goal, float score)> EvaluateAllGoals(List<Goal> goals, AIContext context)
    {
        var results = new List<(Goal goal, float score)>();

        foreach (var goal in goals)
        {
            float score = goal.CalculateScore(context);
            results.Add((goal, score));
        }

        return results.OrderByDescending(x => x.score).ToList();
    }
}
