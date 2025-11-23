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
    /// <param name="conditionType">The condition type (e.g., "armor_buff", "evasion_buff", "regen_buff").</param>
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
            // Stat buffs
            "armor_buff" => new StatBuffCondition(StatType.Armor, amount, duration, durationMode, sourceId),
            "strength_buff" => new StatBuffCondition(StatType.Strength, amount, duration, durationMode, sourceId),
            "agility_buff" => new StatBuffCondition(StatType.Agility, amount, duration, durationMode, sourceId),
            "endurance_buff" => new StatBuffCondition(StatType.Endurance, amount, duration, durationMode, sourceId),
            "evasion_buff" => new StatBuffCondition(StatType.Evasion, amount, duration, durationMode, sourceId),

            // Regen buff
            "regen_buff" => new RegenCondition(amount, duration, durationMode, sourceId),

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
            "armor_buff" => true,
            "strength_buff" => true,
            "agility_buff" => true,
            "endurance_buff" => true,
            "evasion_buff" => true,
            "regen_buff" => true,
            "confusion" => true,
            _ => false
        };
    }
}
