using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// Makes creature flee instead of fight when it sees enemies.
/// Responds to OnIAmBored to push FleeGoal instead of letting BoredGoal push KillTargetGoal.
/// </summary>
public partial class CowardlyComponent : Node, IAIEventHandler
{
    /// <summary>
    /// How many turns to continue fleeing after losing sight of threat.
    /// </summary>
    [Export] public int FleeTurns { get; set; } = 20;

    /// <summary>
    /// Distance at which the creature feels safe and stops fleeing.
    /// </summary>
    [Export] public int SafeDistance { get; set; } = 10;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        var context = args.Context;

        // If we see an enemy, flee instead of fight
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
        {
            var threat = context.GetClosestEnemy(enemies);
            if (threat != null)
            {
                var fleeGoal = new FleeGoal(threat, FleeTurns, SafeDistance);
                context.AIComponent.GoalStack.Push(fleeGoal);
                args.Handled = true; // Don't let BoredGoal push KillTargetGoal
            }
        }
    }
}
