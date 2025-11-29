namespace PitsOfDespair.Data;

/// <summary>
/// Defines an epithet with its stat requirements and display properties.
/// Epithets are titles that describe a player's build archetype based on base stats.
/// </summary>
public class EpithetDefinition
{
    /// <summary>
    /// Display name of the epithet (e.g., "Titan", "Wanderer").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Minimum base STR required (0 = no requirement).
    /// </summary>
    public int RequiredStr { get; init; } = 0;

    /// <summary>
    /// Minimum base AGI required (0 = no requirement).
    /// </summary>
    public int RequiredAgi { get; init; } = 0;

    /// <summary>
    /// Minimum base END required (0 = no requirement).
    /// </summary>
    public int RequiredEnd { get; init; } = 0;

    /// <summary>
    /// Minimum base WIL required (0 = no requirement).
    /// </summary>
    public int RequiredWil { get; init; } = 0;

    /// <summary>
    /// Selection priority. Higher values are preferred when multiple epithets match.
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Atmospheric description for this epithet.
    /// Shown when examining the player.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Checks if the given base stats meet this epithet's requirements.
    /// </summary>
    public bool Matches(int str, int agi, int end, int wil)
        => str >= RequiredStr && agi >= RequiredAgi
        && end >= RequiredEnd && wil >= RequiredWil;

    /// <summary>
    /// Total stat points required across all stats.
    /// Used as a tiebreaker when priorities are equal.
    /// </summary>
    public int TotalRequirement
        => RequiredStr + RequiredAgi + RequiredEnd + RequiredWil;
}
