namespace PitsOfDespair.Debug;

/// <summary>
/// Abstract base class for debug commands.
/// </summary>
public abstract class DebugCommand
{
    /// <summary>
    /// Command name used for invocation (e.g., "give", "heal").
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Human-readable description of what the command does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Usage example showing command syntax (e.g., "give [itemId]").
    /// </summary>
    public abstract string Usage { get; }

    /// <summary>
    /// Execute the debug command.
    /// </summary>
    /// <param name="context">Access to game systems</param>
    /// <param name="args">Command arguments (not including command name)</param>
    /// <returns>Result indicating success/failure and message to display</returns>
    public abstract DebugCommandResult Execute(DebugContext context, string[] args);
}
