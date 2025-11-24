using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Conditions;
using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Captures companion state for persistence across floor transitions.
/// Includes creature ID, health, and active conditions.
/// </summary>
public class CompanionState
{
    /// <summary>
    /// The creature's data file ID (e.g., "cat") for recreation via EntityFactory.
    /// </summary>
    public string CreatureId { get; set; } = "";

    /// <summary>
    /// Current health.
    /// </summary>
    public int CurrentHealth { get; set; }

    /// <summary>
    /// Base maximum Health (before modifiers).
    /// </summary>
    public int BaseMaxHealth { get; set; }

    /// <summary>
    /// Active conditions with their remaining turns.
    /// </summary>
    public List<Condition> ActiveConditions { get; set; } = new();

    /// <summary>
    /// Extracts state from a companion entity.
    /// </summary>
    /// <param name="companion">The companion entity to extract state from.</param>
    /// <returns>CompanionState containing persistent data, or null if extraction fails.</returns>
    public static CompanionState? ExtractFromCompanion(BaseEntity companion)
    {
        // Need creature ID to recreate
        if (string.IsNullOrEmpty(companion.CreatureId))
            return null;

        var state = new CompanionState
        {
            CreatureId = companion.CreatureId
        };

        // Extract health
        var healthComponent = companion.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            state.CurrentHealth = healthComponent.CurrentHealth;
            state.BaseMaxHealth = healthComponent.BaseMaxHealth;
        }

        // Extract conditions (now managed directly by BaseEntity)
        state.ActiveConditions = new List<Condition>(companion.GetActiveConditions());

        return state;
    }

    /// <summary>
    /// Applies this state to a companion entity.
    /// Must be called after the companion's components are initialized.
    /// </summary>
    /// <param name="companion">The companion entity to apply state to.</param>
    public void ApplyToCompanion(BaseEntity companion)
    {
        // Apply health
        var healthComponent = companion.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            healthComponent.BaseMaxHealth = BaseMaxHealth;
            // Use reflection to set CurrentHealth (has private setter)
            SetPrivateProperty(healthComponent, nameof(HealthComponent.CurrentHealth), CurrentHealth);
        }

        // Apply conditions (now managed directly by BaseEntity)
        foreach (var condition in ActiveConditions)
        {
            companion.AddCondition(condition);
        }
    }

    /// <summary>
    /// Sets a private property value using reflection.
    /// </summary>
    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (property != null)
        {
            var setter = property.GetSetMethod(nonPublic: true);
            if (setter != null)
            {
                setter.Invoke(obj, new[] { value });
                return;
            }
        }

        // Try as a backing field
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}
