using System.Collections.Generic;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for yelling for help to alert nearby creatures.
/// Uses FOV to alert only creatures within line of sight (sound doesn't travel through walls).
/// Pushes a KillTargetGoal onto alerted creatures' goal stacks to investigate and attack.
/// </summary>
public class YellForHelpAction : Action
{
    private const int AlertRadius = 10;

    public override string Name => "Yell for Help";

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        // Can always yell if we have the basic requirements
        return actor != null && context != null && context.Player != null;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot yell for help.");
        }

        var player = context.Player;
        var actorPosition = actor.GridPosition;
        int alertedCount = 0;

        // Calculate visible positions from actor using FOV (sound doesn't travel through walls)
        HashSet<GridPosition> visiblePositions = FOVCalculator.CalculateVisibleTiles(
            actorPosition,
            AlertRadius,
            context.MapSystem
        );

        // Alert all allied creatures within visible radius
        var allEntities = context.EntityManager.GetAllEntities();
        foreach (var entity in allEntities)
        {
            // Skip self
            if (entity == actor)
                continue;

            // Only alert allies (same faction)
            if (entity.Faction != actor.Faction)
                continue;

            // Check if entity has AI
            var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
            if (aiComponent == null)
                continue;

            // Check if entity is visible (FOV check - sound doesn't travel through walls)
            if (visiblePositions.Contains(entity.GridPosition))
            {
                // Push a KillTargetGoal to make them attack the player
                var killGoal = new KillTargetGoal(player);
                aiComponent.GoalStack.Push(killGoal);
                alertedCount++;
            }
        }

        // Build message and emit signal for display
        string message = alertedCount > 0
            ? $"The {actor.DisplayName} shrieks for help! {alertedCount} creature(s) are alerted!"
            : $"The {actor.DisplayName} shrieks for help, but no one is nearby.";

        // Emit action message via CombatSystem (orange color for alerts)
        context.CombatSystem.EmitActionMessage(actor, message, Palette.ToHex(Palette.Caution));

        return ActionResult.CreateSuccess(message);
    }
}
