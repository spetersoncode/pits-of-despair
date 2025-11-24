using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// Makes creature wander randomly when it has nothing else to do.
/// Responds to OnIAmBored to push WanderGoal with configurable probability.
/// Only creatures with this component will wander.
/// </summary>
public partial class WanderingComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Chance to wander each turn when bored (0.0 - 1.0).
    /// </summary>
    [Export] public float WanderChance { get; set; } = 0.3f;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        // Don't wander if we see enemies - let combat take precedence
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Random chance to wander
        if (GD.Randf() < WanderChance)
        {
            var wanderGoal = new WanderGoal();
            args.Context.AIComponent.GoalStack.Push(wanderGoal);
            args.Handled = true;
        }
    }
}
