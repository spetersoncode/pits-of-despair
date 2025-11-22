using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Status;
using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Captures companion state for persistence across floor transitions.
/// Includes creature ID, health, and active status effects.
/// </summary>
public class CompanionState
{
    /// <summary>
    /// The creature's data file ID (e.g., "cat") for recreation via EntityFactory.
    /// </summary>
    public string CreatureId { get; set; } = "";

    /// <summary>
    /// Current hit points.
    /// </summary>
    public int CurrentHP { get; set; }

    /// <summary>
    /// Base maximum HP (before modifiers).
    /// </summary>
    public int BaseMaxHP { get; set; }

    /// <summary>
    /// Active status effects with their remaining turns.
    /// </summary>
    public List<Status.Status> ActiveStatuses { get; set; } = new();

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
            state.CurrentHP = healthComponent.CurrentHP;
            state.BaseMaxHP = healthComponent.BaseMaxHP;
        }

        // Extract status effects
        var statusComponent = companion.GetNodeOrNull<StatusComponent>("StatusComponent");
        if (statusComponent != null)
        {
            state.ActiveStatuses = new List<Status.Status>(statusComponent.GetActiveStatuses());
        }

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
            healthComponent.BaseMaxHP = BaseMaxHP;
            // Use reflection to set CurrentHP (has private setter)
            SetPrivateProperty(healthComponent, nameof(HealthComponent.CurrentHP), CurrentHP);
        }

        // Apply status effects
        var statusComponent = companion.GetNodeOrNull<StatusComponent>("StatusComponent");
        if (statusComponent != null)
        {
            foreach (var status in ActiveStatuses)
            {
                statusComponent.AddStatus(status);
            }
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
