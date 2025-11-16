using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for yelling for help to alert nearby creatures.
/// Alerts all creatures within a fixed radius of the player's position,
/// setting their search parameters to investigate.
/// </summary>
public class YellForHelpAction : Action
{
    private const int AlertRadius = 12;
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

        // Alert all creatures within radius
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

            // Check distance from actor
            int distance = DistanceHelper.ChebyshevDistance(actorPosition, entity.GridPosition);
            if (distance <= AlertRadius)
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
