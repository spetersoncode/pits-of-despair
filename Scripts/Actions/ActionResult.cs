namespace PitsOfDespair.Actions;

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
    /// Whether this action consumes a turn. Currently fixed at true for all actions.
    /// </summary>
    public bool ConsumesTurn { get; set; } = true;

    /// <summary>
    /// Creates a successful action result.
    /// </summary>
    public static ActionResult CreateSuccess(string message = "")
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            ConsumesTurn = true
        };
    }

    /// <summary>
    /// Creates a failed action result that doesn't consume a turn.
    /// </summary>
    public static ActionResult CreateFailure(string message = "")
    {
        return new ActionResult
        {
            Success = false,
            Message = message,
            ConsumesTurn = false
        };
    }
}
