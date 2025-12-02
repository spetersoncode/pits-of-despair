using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Helper methods for checking entity conditions.
/// </summary>
public static class ConditionHelper
{
    /// <summary>
    /// Condition types that count as "distracted" for assassin skills.
    /// Distracted enemies are vulnerable to Stab and similar abilities.
    /// </summary>
    private static readonly HashSet<string> DistractedConditions = new()
    {
        "daze",
        "dazzled",
        "blinded",
        "stun",
        "confusion",
        "fear",
        "sleep"
    };

    /// <summary>
    /// Checks if an entity is "distracted" (has a condition that makes them vulnerable).
    /// Used by assassin skills like Stab for bonus damage.
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if the entity has any distracted condition</returns>
    public static bool IsDistracted(BaseEntity? entity)
    {
        if (entity == null) return false;

        var conditions = entity.GetActiveConditions();
        return conditions.Any(c => DistractedConditions.Contains(c.TypeId));
    }

    /// <summary>
    /// Gets all distracted condition type IDs.
    /// </summary>
    public static IReadOnlySet<string> GetDistractedConditionTypes() => DistractedConditions;
}
