namespace PitsOfDespair.Conditions;

/// <summary>
/// Maps stat names (from YAML/skill definitions) to condition type strings.
/// Single source of truth for stat → condition type mapping used by skill processors and equipment.
/// </summary>
public static class StatConditionMapper
{
    /// <summary>
    /// Maps a stat name to its corresponding condition type string.
    /// </summary>
    /// <param name="stat">Stat name (e.g., "str", "strength", "armor", "max_hp")</param>
    /// <returns>Condition type string (e.g., "strength_modifier"), or null if not mappable</returns>
    public static string? GetConditionType(string? stat)
    {
        return stat?.ToLower() switch
        {
            // Primary stats
            "str" or "strength" or "attack" => "strength_modifier",
            "agi" or "agility" or "defense" => "agility_modifier",
            "end" or "endurance" => "endurance_modifier",
            "wil" or "will" or "willpower" => "will_modifier",

            // Combat stats
            "armor" => "armor_modifier",
            "evasion" => "evasion_modifier",
            "melee_damage" => "melee_damage_modifier",

            // Resource stats
            "max_hp" or "hp" => "max_hp_modifier",
            "max_wp" or "wp" => "max_wp_modifier",
            "regen" or "regeneration" => "regen_modifier",

            _ => null
        };
    }

    /// <summary>
    /// Checks if a stat name maps to a valid condition type.
    /// </summary>
    public static bool IsValidStat(string? stat)
    {
        return GetConditionType(stat) != null;
    }
}
