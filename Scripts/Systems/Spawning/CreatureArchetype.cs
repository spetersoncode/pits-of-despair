namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Creature archetypes inferred from stats and capabilities.
/// A creature can have multiple archetypes. Used for encounter composition.
/// </summary>
public enum CreatureArchetype
{
    /// <summary>High END relative to other stats. Frontline defenders.</summary>
    Tank,

    /// <summary>High STR, balanced stats. Standard melee combatants.</summary>
    Warrior,

    /// <summary>High AGI + STR, low END. Glass cannons that deal burst damage.</summary>
    Assassin,

    /// <summary>Has ranged attack OR high AGI + low STR. Attacks from distance.</summary>
    Ranged,

    /// <summary>Has healing effects OR high WIL. Provides buffs/heals to allies.</summary>
    Support,

    /// <summary>High END + STR, low AGI, slow speed. Slow but tough hitters.</summary>
    Brute,

    /// <summary>Has Cowardly or YellForHelp AI. Flees and alerts others.</summary>
    Scout
}
