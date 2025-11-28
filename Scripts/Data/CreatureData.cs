using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Data;

/// <summary>
/// Maps short AI component names used in YAML to full class names.
/// </summary>
public static class AIComponentTypes
{
    private static readonly Dictionary<string, string> TypeMap = new()
    {
        ["Cowardly"] = "CowardlyComponent",
        ["YellForHelp"] = "YellForHelpComponent",
        ["ShootAndScoot"] = "ShootAndScootComponent",
        ["ItemUsage"] = "ItemUsageComponent",
        ["Wandering"] = "WanderingComponent",
        ["ItemCollect"] = "ItemCollectComponent",
        ["Patrol"] = "PatrolComponent",
        ["FollowLeader"] = "FollowLeaderComponent",
        ["JoinPlayerOnSight"] = "JoinPlayerOnSightComponent"
    };

    /// <summary>
    /// Resolves a component type name from YAML to the full class name.
    /// Accepts both short names (Cowardly) and full names (CowardlyComponent).
    /// </summary>
    public static string Resolve(string typeName)
    {
        if (TypeMap.TryGetValue(typeName, out var fullName))
            return fullName;
        return typeName; // Already full name or unknown
    }
}

/// <summary>
/// Serializable creature data structure.
/// Loaded from Data/Creatures/*.yaml sheet files.
/// Type-specific defaults are defined in YAML sheet defaults sections.
/// </summary>
public class CreatureData
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Atmospheric description of the creature.
    /// Used for examine command and creature details.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Creature type for category-based defaults (e.g., "goblinoid", "rodents").
    /// Optional - blank type means no inherited defaults.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Glyph { get; set; } = DataDefaults.UnknownGlyph;

    public string Color { get; set; } = DataDefaults.DefaultColor;

    // Stats
    public int Strength { get; set; } = 0;
    public int Agility { get; set; } = 0;
    public int Endurance { get; set; } = 0;

    [YamlDotNet.Serialization.YamlMember(Alias = "wil")]
    public int Will { get; set; } = 0;

    /// <summary>
    /// Creature threat rating (affects difficulty and XP rewards).
    /// Unbounded scale: 1-5 trivial, 6-15 standard, 16-30 dangerous, 31-50 elite, 51+ boss.
    /// Default threat 1.
    /// </summary>
    public int Threat { get; set; } = 1;

    /// <summary>
    /// Base MaxHealth before Endurance modifiers.
    /// Actual MaxHealth will be: MaxHealth + (Endurance × Level)
    /// </summary>
    [YamlDotNet.Serialization.YamlMember(Alias = "health")]
    public int MaxHealth { get; set; } = 1;

    public int VisionRange { get; set; } = 10;
    
    /// <summary>
    /// Base speed stat. 10 = average speed.
    /// Higher values = faster actions, lower values = slower actions.
    /// Delay = 100 / Speed, so speed 10 = 10 aut delay.
    /// </summary>
    public int Speed { get; set; } = 10;

    public bool HasMovement { get; set; } = true;

    public bool HasAI { get; set; } = true;

    /// <summary>
    /// Faction allegiance of this creature.
    /// Defaults to "Hostile". Can be "Player" or "Neutral".
    /// Player faction creatures are allies that follow and defend the player.
    /// </summary>
    public string Faction { get; set; } = "Hostile";

    public List<AttackData> Attacks { get; set; } = new();

    /// <summary>
    /// List of goal IDs for goal-based AI (e.g., "MeleeAttack", "Wander").
    /// DEPRECATED: Use AIComponents instead. This field is ignored by the new goal-stack AI system.
    /// </summary>
    public List<string> Goals { get; set; } = new();

    /// <summary>
    /// List of AI component configurations for goal-stack AI system.
    /// Each entry is a dictionary with "type" and optional config properties.
    /// Example YAML:
    ///   ai:
    ///     - type: Cowardly
    ///     - type: Patrol
    ///       grouped: true
    ///     - type: Warrior    # Explicit archetype (no behavior, grants archetype)
    /// </summary>
    public List<Dictionary<string, object>> Ai { get; set; } = new();

    /// <summary>
    /// List of equipment entries for this creature.
    /// Supports both simple strings (item ID) and objects with id/quantity properties.
    /// Examples:
    ///   equipment:
    ///     - weapon_club                    # Simple: quantity defaults to 1
    ///     - id: ammo_arrow                 # Object form: quantity defaults to 1
    ///       quantity: 20                   # Override quantity
    /// </summary>
    public List<object> Equipment { get; set; } = new();

    /// <summary>
    /// List of skill IDs this creature knows innately.
    /// Skills are loaded from Data/Skills/*.yaml.
    /// Example:
    ///   skills:
    ///     - magic_missile
    ///     - minor_heal
    /// </summary>
    public List<string> Skills { get; set; } = new();

    /// <summary>
    /// Damage types this creature is immune to (takes 0 damage).
    /// </summary>
    public List<DamageType> Immunities { get; set; } = new();

    /// <summary>
    /// Damage types this creature resists (takes half damage, rounded down).
    /// </summary>
    public List<DamageType> Resistances { get; set; } = new();

    /// <summary>
    /// Damage types this creature is vulnerable to (takes double damage).
    /// </summary>
    public List<DamageType> Vulnerabilities { get; set; } = new();

    /// <summary>
    /// Gets whether this creature can equip items.
    /// Returns true if Equipment list is defined and not empty.
    /// </summary>
    public bool GetCanEquip()
    {
        return Equipment != null && Equipment.Count > 0;
    }

    /// <summary>
    /// Converts this data to a Godot Color object.
    /// </summary>
    public Color GetColor()
    {
        return new Color(Color);
    }

    /// <summary>
    /// Gets the faction as an enum value.
    /// Parses the Faction string, defaulting to Hostile if invalid.
    /// </summary>
    public Core.Faction GetFaction()
    {
        if (System.Enum.TryParse<Core.Faction>(Faction, true, out var result))
        {
            return result;
        }
        return Core.Faction.Hostile;
    }

    /// <summary>
    /// Applies minimal fallback defaults for properties not set via YAML.
    /// Type-specific defaults should be defined in YAML sheet defaults sections.
    /// This method provides last-resort fallbacks only.
    /// </summary>
    public void ApplyDefaults()
    {
        // YAML sheet defaults should handle type-specific values.
        // This method only ensures we have valid fallback values if nothing else was set.

        // No additional logic needed - DataDefaults.UnknownGlyph and DataDefaults.DefaultColor
        // are already set as property defaults and serve as fallbacks.
    }
}
