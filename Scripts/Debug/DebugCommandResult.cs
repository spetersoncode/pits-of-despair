namespace PitsOfDespair.Debug;

/// <summary>
/// Result object returned by debug command execution.
/// </summary>
public class DebugCommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string MessageColor { get; set; }

    public DebugCommandResult(bool success, string message, string messageColor)
    {
        Success = success;
        Message = message;
        MessageColor = messageColor;
    }

    public static DebugCommandResult CreateSuccess(string message, string color)
    {
        return new DebugCommandResult(true, message, color);
    }

    public static DebugCommandResult CreateFailure(string message, string color)
    {
        return new DebugCommandResult(false, message, color);
    }
}
