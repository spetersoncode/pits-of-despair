namespace PitsOfDespair.Actions;

/// <summary>
/// Standard delay cost for actions (in aut - action time units).
/// 10 aut = average speed action.
/// </summary>
public static class ActionDelay
{
    public const int Standard = 10;
}

/// <summary>
/// Represents the result of executing an action.
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Whether the action was successfully executed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing what happened (for message log/UI feedback).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The delay cost of this action in aut (action time units).
    /// 0 means no time passes (failed actions, free actions).
    /// Default is standard delay (10 aut).
    /// </summary>
    public int DelayCost { get; set; } = ActionDelay.Standard;

    /// <summary>
    /// Whether this action costs time (has a delay > 0).
    /// </summary>
    public bool CostsTime => DelayCost > 0;

    /// <summary>
    /// Creates a successful action result with the standard delay cost.
    /// </summary>
    public static ActionResult CreateSuccess(string message = "", int delayCost = ActionDelay.Standard)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            DelayCost = delayCost
        };
    }

    /// <summary>
    /// Creates a failed action result that doesn't cost time.
    /// Failed actions allow the player to retry without penalty.
    /// </summary>
    public static ActionResult CreateFailure(string message = "")
    {
        return new ActionResult
        {
            Success = false,
            Message = message,
            DelayCost = 0
        };
    }
}
