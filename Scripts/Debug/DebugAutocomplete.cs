using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Debug;

/// <summary>
/// Provides autocomplete suggestions for debug commands.
/// </summary>
public static class DebugAutocomplete
{
    /// <summary>
    /// Get all commands that match the given input prefix.
    /// </summary>
    /// <param name="input">The current input text (without leading slash)</param>
    /// <returns>Matching command names in alphabetical order</returns>
    public static IReadOnlyList<string> GetSuggestions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return DebugCommandFactory.GetRegisteredCommands().ToList();
        }

        var lowerInput = input.ToLower();
        return DebugCommandFactory.GetRegisteredCommands()
            .Where(cmd => cmd.StartsWith(lowerInput))
            .ToList();
    }

    /// <summary>
    /// Get the completion suffix for ghost text display.
    /// Returns the remaining characters of the best match after the input.
    /// </summary>
    /// <param name="input">The current input text</param>
    /// <param name="selectedCommand">The currently selected command (or null for best match)</param>
    /// <returns>The suffix to display as ghost text, or empty if no match</returns>
    public static string GetCompletionSuffix(string input, string selectedCommand = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var suggestions = GetSuggestions(input);
        var command = selectedCommand ?? (suggestions.Count > 0 ? suggestions[0] : null);
        if (command == null)
        {
            return string.Empty;
        }

        // Return the part of the command after the input
        if (command.StartsWith(input, System.StringComparison.OrdinalIgnoreCase))
        {
            return command.Substring(input.Length);
        }

        return string.Empty;
    }
}
