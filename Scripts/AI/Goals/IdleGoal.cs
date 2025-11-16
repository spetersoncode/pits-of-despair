using PitsOfDespair.Actions;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal for doing nothing (waiting in place).
/// Ultimate fallback with very low priority - ensures there's always a valid goal.
/// </summary>
public class IdleGoal : Goal
{
    private const float IdleScore = 1f;

    public override float CalculateScore(AIContext context)
    {
        // Always valid as last resort
        return IdleScore;
    }

    public override ActionResult Execute(AIContext context)
    {
        // Clear any existing path since we're idling
        context.AIComponent.ClearPath();

        // Just wait - consume the turn but do nothing
        return new ActionResult
        {
            Success = true,
            Message = "Idling",
            ConsumesTurn = true
        };
    }

    public override string GetName() => "Idle";
}
