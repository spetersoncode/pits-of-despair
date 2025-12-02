using System.Collections.Generic;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Helper for English pluralization of game entity names.
/// </summary>
public static class Pluralizer
{
    /// <summary>
    /// Common irregular plurals for roguelike creatures and items.
    /// </summary>
    private static readonly Dictionary<string, string> Irregulars = new(System.StringComparer.OrdinalIgnoreCase)
    {
        { "wolf", "wolves" },
        { "thief", "thieves" },
        { "knife", "knives" },
        { "staff", "staves" },
        { "dwarf", "dwarves" },
        { "elf", "elves" },
        { "self", "selves" },
        { "leaf", "leaves" },
        { "man", "men" },
        { "woman", "women" },
        { "child", "children" },
        { "foot", "feet" },
        { "tooth", "teeth" },
        { "goose", "geese" },
        { "mouse", "mice" },
        { "louse", "lice" },
        { "ox", "oxen" },
        { "fish", "fish" },
        { "sheep", "sheep" },
        { "deer", "deer" },
        { "moose", "moose" },
        { "swine", "swine" },
    };

    /// <summary>
    /// Returns the plural form of a word.
    /// </summary>
    /// <param name="word">The singular word to pluralize.</param>
    /// <returns>The plural form of the word.</returns>
    public static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Check irregular plurals
        if (Irregulars.TryGetValue(word, out string irregular))
            return irregular;

        string lower = word.ToLowerInvariant();

        // Words ending in s, x, z, ch, sh -> add "es"
        if (lower.EndsWith("s") || lower.EndsWith("x") || lower.EndsWith("z") ||
            lower.EndsWith("ch") || lower.EndsWith("sh"))
            return word + "es";

        // Words ending in consonant + y -> change y to ies
        if (lower.EndsWith("y") && lower.Length > 1)
        {
            char beforeY = lower[lower.Length - 2];
            if (!"aeiou".Contains(beforeY))
                return word.Substring(0, word.Length - 1) + "ies";
        }

        // Default: add "s"
        return word + "s";
    }

    /// <summary>
    /// Returns the singular or plural form based on count.
    /// </summary>
    /// <param name="word">The singular word.</param>
    /// <param name="count">The count to check.</param>
    /// <returns>Singular if count is 1, plural otherwise.</returns>
    public static string PluralizeIf(string word, int count)
    {
        return count == 1 ? word : Pluralize(word);
    }

    /// <summary>
    /// Returns a formatted string with count and appropriate singular/plural form.
    /// Example: FormatCount("rat", 3) returns "3 rats"
    /// </summary>
    /// <param name="word">The singular word.</param>
    /// <param name="count">The count.</param>
    /// <returns>Formatted string with count and word.</returns>
    public static string FormatCount(string word, int count)
    {
        return $"{count} {PluralizeIf(word, count)}";
    }
}
