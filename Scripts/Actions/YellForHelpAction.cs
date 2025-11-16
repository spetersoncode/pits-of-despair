using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for yelling for help to alert nearby creatures.
/// Uses FOV to alert only creatures within line of sight (sound doesn't travel through walls).
/// Sets alerted creatures' search parameters to investigate the player's position.
/// </summary>
public class YellForHelpAction : Action
{
    private const int AlertRadius = 16;
    private const int AlertSearchTurns = 12;

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

        var playerPosition = context.Player.GridPosition;
        var actorPosition = actor.GridPosition;
        int alertedCount = 0;

        // Calculate visible positions from actor using FOV (sound doesn't travel through walls)
        HashSet<GridPosition> visiblePositions = FOVCalculator.CalculateVisibleTiles(
            actorPosition,
            AlertRadius,
            context.MapSystem
        );

        // Alert all creatures within visible radius
        var allEntities = context.EntityManager.GetAllEntities();
        foreach (var entity in allEntities)
        {
            // Skip self
            if (entity == actor)
            {
                continue;
            }

            // Check if entity has AI
            var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
            if (aiComponent == null)
            {
                continue;
            }

            // Check if entity is visible (FOV check - sound doesn't travel through walls)
            if (visiblePositions.Contains(entity.GridPosition))
            {
                // Alert this creature to the player's position
                aiComponent.LastKnownPlayerPosition = playerPosition;
                aiComponent.SearchTurnsRemaining = AlertSearchTurns;
                // Reset turns since player seen so SearchLastKnown goal has high priority
                aiComponent.TurnsSincePlayerSeen = 0;
                alertedCount++;
            }
        }

        // Build message and emit signal for display
        string message = alertedCount > 0
            ? $"The {actor.DisplayName} shrieks for help! {alertedCount} creature(s) are alerted!"
            : $"The {actor.DisplayName} shrieks for help, but no one is nearby.";

        // Emit action message via CombatSystem (orange color for alerts)
        context.CombatSystem.EmitActionMessage(actor, message, "#ffaa00");

        return ActionResult.CreateSuccess(message);
    }
}
