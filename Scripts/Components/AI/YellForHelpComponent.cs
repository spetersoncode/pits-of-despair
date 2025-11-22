using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// Causes creature to periodically yell for help, alerting nearby allies.
/// Responds to OnGetDefensiveActions during combat/fleeing.
/// </summary>
public partial class YellForHelpComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Minimum turns between yells.
    /// </summary>
    [Export] public int YellCooldown { get; set; } = 4;

    private int _turnsSinceYell = 0;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnGetDefensiveActions)
            return;

        _turnsSinceYell++;

        if (_turnsSinceYell >= YellCooldown)
        {
            // High weight - yelling is important when fleeing
            args.ActionList.Add(
                weight: 100,
                execute: ctx => YellForHelp(ctx),
                debugName: "Yell for help"
            );
        }
    }

    private void YellForHelp(AIContext context)
    {
        _turnsSinceYell = 0;

        var yellAction = new YellForHelpAction();
        context.Entity.ExecuteAction(yellAction, context.ActionContext);
    }
}
