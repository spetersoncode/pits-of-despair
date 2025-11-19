namespace PitsOfDespair.Data;

/// <summary>
/// Represents the type of damage dealt by an attack.
/// Physical damage types include slashing (blades), piercing (spears/arrows), and bludgeoning (clubs/fists).
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Bludgeoning damage from blunt force impacts (clubs, maces, fists, crushing).
    /// This is the default damage type for attacks that don't specify a type.
    /// </summary>
    Bludgeoning,

    /// <summary>
    /// Slashing damage from bladed weapons (swords, axes, claws).
    /// </summary>
    Slashing,

    /// <summary>
    /// Piercing damage from pointed weapons (spears, arrows, stingers).
    /// </summary>
    Piercing,

    /// <summary>
    /// Poison damage from toxins, venom, and poisonous substances.
    /// </summary>
    Poison
}
