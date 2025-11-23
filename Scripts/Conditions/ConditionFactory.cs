using Godot;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Single source of truth for creating Condition instances from type strings.
/// Used by effects, reactive skills, equipment, and any other system that needs to apply conditions.
/// </summary>
public static class ConditionFactory
{
    /// <summary>
    /// Creates a condition instance from a type string.
    /// </summary>
    /// <param name="conditionType">The condition type (e.g., "armor_modifier", "evasion_modifier", "regen_modifier").</param>
    /// <param name="amount">The amount/magnitude of the condition (if applicable).</param>
    /// <param name="duration">Duration as dice notation (e.g., "10", "2d3").</param>
    /// <param name="durationMode">Duration mode (Temporary, Permanent, WhileEquipped, WhileActive).</param>
    /// <param name="sourceId">Optional source identifier for tracking condition origin.</param>
    /// <returns>A new Condition instance, or null if the type is unknown.</returns>
    public static Condition? Create(
        string? conditionType,
        int amount = 0,
        string duration = "1",
        ConditionDuration durationMode = ConditionDuration.Temporary,
        string? sourceId = null)
    {
        if (string.IsNullOrEmpty(conditionType))
        {
            GD.PrintErr("ConditionFactory: conditionType is null or empty");
            return null;
        }

        Condition? condition = conditionType.ToLower() switch
        {
            // Stat modifiers
            "armor_modifier" => new StatModifierCondition(StatType.Armor, amount, duration, durationMode, sourceId),
            "strength_modifier" => new StatModifierCondition(StatType.Strength, amount, duration, durationMode, sourceId),
            "agility_modifier" => new StatModifierCondition(StatType.Agility, amount, duration, durationMode, sourceId),
            "endurance_modifier" => new StatModifierCondition(StatType.Endurance, amount, duration, durationMode, sourceId),
            "will_modifier" => new StatModifierCondition(StatType.Will, amount, duration, durationMode, sourceId),
            "evasion_modifier" => new StatModifierCondition(StatType.Evasion, amount, duration, durationMode, sourceId),

            // Regen modifier
            "regen_modifier" => new RegenCondition(amount, duration, durationMode, sourceId),

            // Debuffs / Status effects
            "confusion" => new ConfusionCondition(duration),

            _ => null
        };

        if (condition == null)
        {
            GD.PrintErr($"ConditionFactory: Unknown condition type '{conditionType}'");
        }

        return condition;
    }

    /// <summary>
    /// Creates a condition instance from a type string, with integer duration.
    /// Convenience overload for systems that track duration as integers.
    /// </summary>
    public static Condition? Create(string? conditionType, int amount, int duration)
    {
        return Create(conditionType, amount, duration.ToString());
    }

    /// <summary>
    /// Checks if a condition type string is valid and recognized.
    /// </summary>
    public static bool IsValidType(string? conditionType)
    {
        if (string.IsNullOrEmpty(conditionType))
            return false;

        return conditionType.ToLower() switch
        {
            "armor_modifier" => true,
            "strength_modifier" => true,
            "agility_modifier" => true,
            "endurance_modifier" => true,
            "will_modifier" => true,
            "evasion_modifier" => true,
            "regen_modifier" => true,
            "confusion" => true,
            _ => false
        };
    }
}
