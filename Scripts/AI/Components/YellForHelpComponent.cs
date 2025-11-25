using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Components;

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
    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
    }

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnGetDefensiveActions)
            return;

        _turnsSinceYell++;

        if (_turnsSinceYell >= YellCooldown)
        {
            // Reset cooldown now - we're offering the action
            // The action might not be picked (weighted random), but that's fine
            // since the creature is actively considering yelling
            _turnsSinceYell = 0;

            var yellAction = new YellForHelpAction();
            var aiAction = new AIAction(
                action: yellAction,
                weight: 100,
                debugName: "Yell for help"
            );
            args.ActionList.Add(aiAction);
        }
    }
}
