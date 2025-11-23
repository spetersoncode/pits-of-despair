using PitsOfDespair.Entities;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for waiting/resting for one turn.
/// Simply passes the turn - regeneration happens passively via HealthComponent.
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

        // Regeneration now happens passively each turn via HealthComponent.
        // Wait action simply passes the turn.

        // Emit feedback for player
        if (actor is Player player)
        {
            player.EmitWaitFeedback();
        }

        return ActionResult.CreateSuccess();
    }
}
