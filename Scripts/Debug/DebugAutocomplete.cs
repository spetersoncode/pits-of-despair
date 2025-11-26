using System;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Debug;

/// <summary>
/// Provides autocomplete suggestions for debug commands and their arguments.
/// </summary>
public static class DebugAutocomplete
{
    /// <summary>
    /// Result of parsing input for autocomplete context.
    /// </summary>
    public readonly struct AutocompleteContext
    {
        /// <summary>
        /// Whether we're completing a command name (false) or an argument (true).
        /// </summary>
        public bool IsArgumentContext { get; init; }

        /// <summary>
        /// The command name (if in argument context).
        /// </summary>
        public string CommandName { get; init; }

        /// <summary>
        /// The argument index being completed (0-based).
        /// </summary>
        public int ArgIndex { get; init; }

        /// <summary>
        /// The current partial value being typed.
        /// </summary>
        public string CurrentValue { get; init; }
    }

    /// <summary>
    /// Parse input to determine autocomplete context.
    /// </summary>
    /// <param name="input">The full input text (without leading slash)</param>
    /// <returns>Context describing what should be autocompleted</returns>
    public static AutocompleteContext ParseContext(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains(' '))
        {
            // No space means we're still typing the command name
            return new AutocompleteContext
            {
                IsArgumentContext = false,
                CommandName = null,
                ArgIndex = -1,
                CurrentValue = input ?? string.Empty
            };
        }

        // Split into parts - command and arguments
        var parts = input.Split(' ', StringSplitOptions.None);
        var commandName = parts[0];

        // Count completed arguments (non-empty parts after command)
        // The last part is what's being typed
        var argParts = parts.Skip(1).ToList();

        // If input ends with space, we're starting a new argument
        if (input.EndsWith(' '))
        {
            return new AutocompleteContext
            {
                IsArgumentContext = true,
                CommandName = commandName,
                ArgIndex = argParts.Count(p => !string.IsNullOrEmpty(p)),
                CurrentValue = string.Empty
            };
        }

        // Otherwise we're typing the last argument
        var currentValue = argParts.LastOrDefault() ?? string.Empty;
        var argIndex = argParts.Count - 1;
        if (argIndex < 0) argIndex = 0;

        return new AutocompleteContext
        {
            IsArgumentContext = true,
            CommandName = commandName,
            ArgIndex = argIndex,
            CurrentValue = currentValue
        };
    }

    /// <summary>
    /// Get suggestions based on input context.
    /// Handles both command name completion and argument completion.
    /// </summary>
    /// <param name="input">The current input text (without leading slash)</param>
    /// <returns>Matching suggestions</returns>
    public static IReadOnlyList<string> GetSuggestions(string input)
    {
        var context = ParseContext(input);

        if (!context.IsArgumentContext)
        {
            return GetCommandSuggestions(context.CurrentValue);
        }

        return GetArgumentSuggestions(context.CommandName, context.ArgIndex, context.CurrentValue)
            ?? new List<string>();
    }

    /// <summary>
    /// Get command name suggestions matching the prefix.
    /// </summary>
    public static IReadOnlyList<string> GetCommandSuggestions(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return DebugCommandFactory.GetRegisteredCommands().ToList();
        }

        var lowerPrefix = prefix.ToLower();
        return DebugCommandFactory.GetRegisteredCommands()
            .Where(cmd => cmd.Contains(lowerPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Get argument suggestions for a specific command and argument index.
    /// </summary>
    public static IReadOnlyList<string> GetArgumentSuggestions(string commandName, int argIndex, string currentValue)
    {
        return DebugCommandFactory.GetArgumentSuggestions(commandName, argIndex, currentValue);
    }

}
