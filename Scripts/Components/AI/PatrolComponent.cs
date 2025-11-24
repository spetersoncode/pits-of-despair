using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// Makes creature patrol to random distant locations when bored.
/// Responds to OnIAmBored to push PatrolGoal with configurable probability and distance.
/// Only creatures with this component will patrol.
/// </summary>
public partial class PatrolComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Chance to start patrolling each turn when bored (0.0 - 1.0).
    /// </summary>
    [Export] public float PatrolChance { get; set; } = 0.3f;

    /// <summary>
    /// Minimum distance for patrol destinations.
    /// </summary>
    [Export] public int MinDistance { get; set; } = 10;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        // Don't patrol if we see enemies - combat takes precedence
        var enemies = args.Context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Random chance to patrol
        if (GD.Randf() < PatrolChance)
        {
            var patrolGoal = new PatrolGoal(MinDistance);
            args.Context.AIComponent.GoalStack.Push(patrolGoal);
            args.Handled = true;
        }
    }
}
