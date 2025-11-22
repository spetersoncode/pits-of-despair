namespace PitsOfDespair.Core;

/// <summary>
/// Defines the faction allegiance of an entity.
/// Determines combat targeting and AI behavior.
/// </summary>
public enum Faction
{
    /// <summary>
    /// Hostile to the player and friendly entities.
    /// Default faction for enemies.
    /// </summary>
    Hostile,

    /// <summary>
    /// Allied with the player. Will not attack player or other friendlies.
    /// Used for summoned creatures, rescued prisoners, etc.
    /// </summary>
    Friendly,

    /// <summary>
    /// Neutral entities that don't participate in combat.
    /// Will not attack or be attacked by default.
    /// </summary>
    Neutral
}

/// <summary>
/// Extension methods for Faction enum.
/// </summary>
public static class FactionExtensions
{
    /// <summary>
    /// Determines if this faction considers the other faction hostile.
    /// </summary>
    public static bool IsHostileTo(this Faction self, Faction other)
    {
        return self switch
        {
            Faction.Hostile => other == Faction.Friendly,
            Faction.Friendly => other == Faction.Hostile,
            Faction.Neutral => false,
            _ => false
        };
    }

    /// <summary>
    /// Determines if this faction considers the other faction friendly (same side).
    /// </summary>
    public static bool IsFriendlyTo(this Faction self, Faction other)
    {
        return self switch
        {
            Faction.Hostile => other == Faction.Hostile,
            Faction.Friendly => other == Faction.Friendly,
            Faction.Neutral => false,
            _ => false
        };
    }
}
