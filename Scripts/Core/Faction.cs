namespace PitsOfDespair.Core;

/// <summary>
/// Defines the faction allegiance of an entity.
/// Determines combat targeting and AI behavior.
/// </summary>
public enum Faction
{
    /// <summary>
    /// Hostile to the player and player-allied entities.
    /// Default faction for enemies.
    /// </summary>
    Hostile,

    /// <summary>
    /// The player and player-allied entities.
    /// Used for the player, companions, summoned creatures, etc.
    /// </summary>
    Player,

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
            Faction.Hostile => other == Faction.Player,
            Faction.Player => other == Faction.Hostile,
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
            Faction.Player => other == Faction.Player,
            Faction.Neutral => false,
            _ => false
        };
    }
}
