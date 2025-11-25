using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Data;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Infers creature archetypes from their stats and capabilities.
/// Archetypes are used to match creatures to encounter slot requirements.
/// </summary>
public static class ArchetypeInferrer
{
    /// <summary>
    /// Infers all applicable archetypes for a creature.
    /// A creature can have multiple archetypes.
    /// </summary>
    public static List<CreatureArchetype> InferArchetypes(CreatureData creature)
    {
        var archetypes = new List<CreatureArchetype>();

        // Stat-based archetypes (can overlap)
        int str = creature.Strength;
        int agi = creature.Agility;
        int end = creature.Endurance;
        int wil = creature.Will;

        // Find the highest stat for relative comparisons
        int maxStat = new[] { str, agi, end, wil }.Max();
        int minStat = new[] { str, agi, end, wil }.Min();

        // Tank: High END relative to other stats
        if (end >= 1 && end >= str && end >= agi)
        {
            archetypes.Add(CreatureArchetype.Tank);
        }

        // Warrior: High STR, balanced or positive stats
        if (str >= 1 && str >= maxStat - 1)
        {
            archetypes.Add(CreatureArchetype.Warrior);
        }

        // Assassin: High AGI + positive STR, lower END
        if (agi >= 1 && str >= 0 && end < agi)
        {
            archetypes.Add(CreatureArchetype.Assassin);
        }

        // Ranged: Must have actual ranged capability (attack or weapon)
        if (HasRangedCapability(creature))
        {
            archetypes.Add(CreatureArchetype.Ranged);
        }

        // Support: High WIL (future: has healing effects)
        if (wil >= 2)
        {
            archetypes.Add(CreatureArchetype.Support);
        }

        // Brute: High STR + high END, low AGI, often slow
        if (str >= 1 && end >= 1 && agi <= 0)
        {
            archetypes.Add(CreatureArchetype.Brute);
        }

        // Cowardly: Has Cowardly AI component
        if (HasCowardlyBehavior(creature))
        {
            archetypes.Add(CreatureArchetype.Cowardly);
        }

        // Patroller: Has Patrol AI component
        if (HasPatrolBehavior(creature))
        {
            archetypes.Add(CreatureArchetype.Patroller);
        }

        // Explicit archetype declarations from AI types
        // e.g., type: Warrior grants Warrior archetype directly
        if (creature.Ai != null)
        {
            foreach (var aiConfig in creature.Ai)
            {
                var typeName = GetAiTypeName(aiConfig);
                if (typeName != null && TryParseArchetype(typeName, out var explicitArchetype))
                {
                    // Only add if the type name exactly matches an archetype
                    // (not inferred behavior like Cowardly/Patrol which are handled above)
                    if (!archetypes.Contains(explicitArchetype))
                    {
                        archetypes.Add(explicitArchetype);
                    }
                }
            }
        }

        // Default to Warrior if no archetypes assigned
        if (archetypes.Count == 0)
        {
            archetypes.Add(CreatureArchetype.Warrior);
        }

        return archetypes;
    }

    /// <summary>
    /// Extracts the AI type name from a config dictionary.
    /// </summary>
    private static string GetAiTypeName(Dictionary<string, object> config)
    {
        if (config == null)
            return null;

        if (config.TryGetValue("type", out var typeObj))
            return typeObj?.ToString();

        return null;
    }

    /// <summary>
    /// Checks if a creature has ranged attack capability.
    /// </summary>
    private static bool HasRangedCapability(CreatureData creature)
    {
        // Check attacks for ranged type
        if (creature.Attacks != null)
        {
            foreach (var attack in creature.Attacks)
            {
                if (attack.Type == AttackType.Ranged)
                {
                    return true;
                }
            }
        }

        // Check equipment for ranged weapons
        if (creature.Equipment != null)
        {
            foreach (var equip in creature.Equipment)
            {
                // Equipment entries can be string IDs or Dictionary objects
                string itemId = null;

                if (equip is string strId)
                {
                    itemId = strId;
                }
                else if (equip is Dictionary<object, object> dict && dict.TryGetValue("id", out var idObj))
                {
                    itemId = idObj?.ToString();
                }
                else
                {
                    itemId = equip?.ToString();
                }

                if (itemId != null && (
                    itemId.Contains("bow") ||
                    itemId.Contains("crossbow") ||
                    itemId.Contains("sling") ||
                    itemId.Contains("thrown")))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a creature has Cowardly AI behavior.
    /// </summary>
    private static bool HasCowardlyBehavior(CreatureData creature)
    {
        if (creature.Ai == null || creature.Ai.Count == 0)
            return false;

        foreach (var aiConfig in creature.Ai)
        {
            var typeName = GetAiTypeName(aiConfig);
            if (string.Equals(typeName, "Cowardly", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a creature has Patrol AI behavior.
    /// </summary>
    private static bool HasPatrolBehavior(CreatureData creature)
    {
        if (creature.Ai == null || creature.Ai.Count == 0)
            return false;

        foreach (var aiConfig in creature.Ai)
        {
            var typeName = GetAiTypeName(aiConfig);
            if (string.Equals(typeName, "Patrol", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a creature matches any of the preferred archetypes.
    /// </summary>
    public static bool MatchesArchetypes(CreatureData creature, List<string> preferredArchetypes)
    {
        if (preferredArchetypes == null || preferredArchetypes.Count == 0)
            return true; // No preference means any archetype matches

        var creatureArchetypes = InferArchetypes(creature);

        foreach (var preferred in preferredArchetypes)
        {
            if (TryParseArchetype(preferred, out var archetype))
            {
                if (creatureArchetypes.Contains(archetype))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates how well a creature matches preferred archetypes.
    /// Returns 0-100 score.
    /// </summary>
    public static int CalculateArchetypeMatchScore(CreatureData creature, List<string> preferredArchetypes)
    {
        if (preferredArchetypes == null || preferredArchetypes.Count == 0)
            return 50; // Neutral score for no preference

        var creatureArchetypes = InferArchetypes(creature);
        int matchCount = 0;

        foreach (var preferred in preferredArchetypes)
        {
            if (TryParseArchetype(preferred, out var archetype))
            {
                if (creatureArchetypes.Contains(archetype))
                    matchCount++;
            }
        }

        if (matchCount == 0)
            return 10; // Low but non-zero score

        // Score based on match ratio
        return 50 + (matchCount * 50 / preferredArchetypes.Count);
    }

    /// <summary>
    /// Tries to parse a string to a CreatureArchetype.
    /// </summary>
    private static bool TryParseArchetype(string value, out CreatureArchetype archetype)
    {
        archetype = value?.ToLowerInvariant() switch
        {
            "tank" => CreatureArchetype.Tank,
            "warrior" => CreatureArchetype.Warrior,
            "assassin" => CreatureArchetype.Assassin,
            "ranged" => CreatureArchetype.Ranged,
            "support" => CreatureArchetype.Support,
            "brute" => CreatureArchetype.Brute,
            "cowardly" => CreatureArchetype.Cowardly,
            "patroller" => CreatureArchetype.Patroller,
            _ => CreatureArchetype.Warrior
        };

        return value != null;
    }

    /// <summary>
    /// Gets a readable string of archetypes for debugging.
    /// </summary>
    public static string GetArchetypeString(CreatureData creature)
    {
        var archetypes = InferArchetypes(creature);
        return string.Join(", ", archetypes.Select(a => a.ToString()));
    }
}
