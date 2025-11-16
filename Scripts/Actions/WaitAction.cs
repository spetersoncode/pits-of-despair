using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for waiting/resting for one turn.
/// Heals the actor by 1 HP if they have a HealthComponent.
/// </summary>
public class WaitAction : Action
{
    public override string Name => "Wait";

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        // Wait action is always valid
        return actor != null && context != null;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot wait.");
        }

        // Heal the actor if they have health
        var healthComponent = actor.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            healthComponent.Heal(1);
        }

        // Emit feedback for player
        if (actor is Player player)
        {
            player.EmitWaitFeedback();
        }

        return ActionResult.CreateSuccess();
    }
}
