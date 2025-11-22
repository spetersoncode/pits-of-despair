using PitsOfDespair.Entities;

namespace PitsOfDespair.AI;

/// <summary>
/// Event data for AI action gathering requests.
/// Components add their available actions to the ActionList.
/// </summary>
public class GetActionsEventArgs
{
    /// <summary>
    /// List where components add their available actions.
    /// </summary>
    public WeightedActionList ActionList { get; } = new WeightedActionList();

    /// <summary>
    /// The AI context for this turn.
    /// </summary>
    public AIContext Context { get; set; }

    /// <summary>
    /// Target entity for attack actions. May be null for non-targeted events.
    /// </summary>
    public BaseEntity Target { get; set; }

    /// <summary>
    /// Set to true to stop event propagation.
    /// Used by OnIAmBored handlers to prevent default behavior.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Event names for AI action gathering.
/// Fire these events on the entity to gather available actions from components.
/// </summary>
public static class AIEvents
{
    /// <summary>
    /// Fired when looking for melee attack options.
    /// Components with melee attacks add them to ActionList.
    /// </summary>
    public const string OnGetMeleeActions = "OnGetMeleeActions";

    /// <summary>
    /// Fired when looking for defensive options (healing, blocking, etc).
    /// Components add defensive actions to ActionList.
    /// </summary>
    public const string OnGetDefensiveActions = "OnGetDefensiveActions";

    /// <summary>
    /// Fired when looking for ranged attack options.
    /// Components with ranged attacks add them to ActionList.
    /// </summary>
    public const string OnGetRangedActions = "OnGetRangedActions";

    /// <summary>
    /// Fired when looking for item usage options.
    /// Inventory items add their usable actions to ActionList.
    /// </summary>
    public const string OnGetItemActions = "OnGetItemActions";

    /// <summary>
    /// Fired when looking for movement options.
    /// Special movement abilities add themselves to ActionList.
    /// </summary>
    public const string OnGetMovementActions = "OnGetMovementActions";

    /// <summary>
    /// Fired when the creature has nothing to do.
    /// Components can push goals onto the stack or set Handled = true.
    /// </summary>
    public const string OnIAmBored = "OnIAmBored";

    /// <summary>
    /// Fired after a successful ranged attack.
    /// Used by ShootAndScootComponent to trigger tactical retreat.
    /// </summary>
    public const string OnRangedAttackSuccess = "OnRangedAttackSuccess";
}
