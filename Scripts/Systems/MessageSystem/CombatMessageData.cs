using System.Collections.Generic;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Represents a property effect that occurred during combat.
/// </summary>
public struct PropertyEffectData
{
    public string Verb { get; set; }      // e.g., "scorched", "shocked", "frozen"
    public int Damage { get; set; }
    public string Color { get; set; }
}

/// <summary>
/// Holds combat event data for a single target, allowing multiple events
/// to be combined into a single readable message.
/// </summary>
public class CombatMessageData
{
    /// <summary>
    /// The entity that performed the action (attacker/caster).
    /// </summary>
    public BaseEntity Attacker { get; set; }

    /// <summary>
    /// The entity that received damage.
    /// </summary>
    public BaseEntity Target { get; set; }

    /// <summary>
    /// The amount of damage dealt (after modifiers).
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The type of damage dealt.
    /// </summary>
    public DamageType DamageType { get; set; }

    /// <summary>
    /// The name of the weapon, skill, or attack used.
    /// </summary>
    public string SourceName { get; set; }

    /// <summary>
    /// Whether this was a skill/spell (vs melee/ranged weapon attack).
    /// </summary>
    public bool IsSkill { get; set; }

    /// <summary>
    /// The damage modifier applied, if any (null, "immune", "resisted", "vulnerable").
    /// </summary>
    public string Modifier { get; set; }

    /// <summary>
    /// Whether the target died from this damage.
    /// </summary>
    public bool TargetDied { get; set; }

    /// <summary>
    /// Whether the attack was blocked by armor (hit but 0 damage).
    /// </summary>
    public bool WasBlocked { get; set; }

    /// <summary>
    /// Whether the attack missed entirely.
    /// </summary>
    public bool WasMissed { get; set; }

    /// <summary>
    /// XP reward if the target died and attacker is player.
    /// </summary>
    public int XPReward { get; set; }

    /// <summary>
    /// Property effects that triggered on this attack (elemental damage, lifesteal, etc.).
    /// </summary>
    public List<PropertyEffectData> PropertyEffects { get; set; } = new();

    /// <summary>
    /// Healing received from vampiric effects.
    /// </summary>
    public int LifestealHealing { get; set; }

    /// <summary>
    /// The target's maximum HP at time of attack (for severity calculation).
    /// </summary>
    public int TargetMaxHP { get; set; }

    /// <summary>
    /// The target's HP after damage (for injury state display).
    /// </summary>
    public int TargetCurrentHP { get; set; }

    /// <summary>
    /// Creates a unique key for grouping messages by attacker-target-source combination.
    /// </summary>
    public string GetGroupKey()
    {
        return $"{Attacker?.GetInstanceId()}_{Target?.GetInstanceId()}_{SourceName}";
    }
}
