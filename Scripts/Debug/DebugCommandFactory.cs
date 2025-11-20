using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Debug.Commands;

namespace PitsOfDespair.Debug;

/// <summary>
/// Factory for creating debug commands using string-based registration.
/// </summary>
public static class DebugCommandFactory
{
    private static readonly Dictionary<string, Func<DebugCommand>> _commandRegistry = new()
    {
        { "give", () => new GiveCommand() },
        { "help", () => new HelpCommand() },
        { "reveal", () => new RevealCommand() },
        { "stairs", () => new StairsCommand() }
    };

    /// <summary>
    /// Create a debug command by name.
    /// </summary>
    /// <param name="commandName">Name of the command to create</param>
    /// <returns>Command instance, or null if not found</returns>
    public static DebugCommand CreateCommand(string commandName)
    {
        var lowerName = commandName.ToLower();

        if (_commandRegistry.TryGetValue(lowerName, out var factory))
        {
            return factory();
        }

        GD.PrintErr($"[DebugCommandFactory] Unknown command: {commandName}");
        return null;
    }

    /// <summary>
    /// Get all registered command names.
    /// </summary>
    public static IEnumerable<string> GetRegisteredCommands()
    {
        return _commandRegistry.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Get all command instances for inspection (e.g., help system).
    /// </summary>
    public static IEnumerable<DebugCommand> GetAllCommands()
    {
        return _commandRegistry.Values.Select(factory => factory()).OrderBy(cmd => cmd.Name);
    }
}
