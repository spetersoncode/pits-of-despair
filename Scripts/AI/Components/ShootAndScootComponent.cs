using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// AI component that causes creatures to retreat after successful ranged attacks.
/// Responds to OnRangedAttackSuccess by pushing a FleeGoal onto the stack.
/// This creates "kiting" behavior for ranged creatures.
/// </summary>
public partial class ShootAndScootComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Number of turns to flee after a successful ranged attack.
    /// </summary>
    [Export] public int FleeTurns { get; set; } = 3;

    /// <summary>
    /// Distance to try to maintain from the target.
    /// </summary>
    [Export] public int FleeDistance { get; set; } = 4;

    /// <summary>
    /// Handle AI events - responds to OnRangedAttackSuccess.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnRangedAttackSuccess)
        {
            return;
        }

        if (args.Target == null || args.Context?.AIComponent == null)
        {
            return;
        }

        var ai = args.Context.AIComponent;
        var currentGoal = ai.GoalStack.Peek();

        // Push flee goal onto stack - will execute before returning to combat
        var fleeGoal = new FleeGoal(
            args.Target,
            FleeTurns,
            FleeDistance,
            originalIntent: currentGoal);

        ai.GoalStack.Push(fleeGoal);
    }
}
