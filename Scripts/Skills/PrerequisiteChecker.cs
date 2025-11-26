using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Data;

namespace PitsOfDespair.Skills;

/// <summary>
/// Utility class for checking skill prerequisites against entity stats.
/// </summary>
public static class PrerequisiteChecker
{
    /// <summary>
    /// Checks if an entity meets all prerequisites for a skill.
    /// </summary>
    /// <param name="skill">The skill definition to check</param>
    /// <param name="stats">The entity's stats component</param>
    /// <param name="levelOverride">Optional level override for simulation (uses stats.Level if null)</param>
    /// <returns>True if all prerequisites are met</returns>
    public static bool MeetsPrerequisites(SkillDefinition skill, StatsComponent stats, int? levelOverride = null)
    {
        if (skill == null || stats == null)
            return false;

        var prereqs = skill.Prerequisites;
        int effectiveLevel = levelOverride ?? stats.Level;

        return effectiveLevel >= prereqs.Level
            && stats.BaseStrength >= prereqs.Str
            && stats.BaseAgility >= prereqs.Agi
            && stats.BaseEndurance >= prereqs.End
            && stats.BaseWill >= prereqs.Wil;
    }

    /// <summary>
    /// Gets a dictionary of missing prerequisites for a skill.
    /// </summary>
    /// <param name="skill">The skill definition to check</param>
    /// <param name="stats">The entity's stats component</param>
    /// <param name="levelOverride">Optional level override for simulation (uses stats.Level if null)</param>
    /// <returns>Dictionary of stat name -> amount needed, empty if all prereqs met</returns>
    public static Dictionary<string, int> GetMissingPrerequisites(SkillDefinition skill, StatsComponent stats, int? levelOverride = null)
    {
        var missing = new Dictionary<string, int>();

        if (skill == null || stats == null)
            return missing;

        var prereqs = skill.Prerequisites;
        int effectiveLevel = levelOverride ?? stats.Level;

        if (effectiveLevel < prereqs.Level)
            missing["LVL"] = prereqs.Level - effectiveLevel;

        if (stats.BaseStrength < prereqs.Str)
            missing["STR"] = prereqs.Str - stats.BaseStrength;

        if (stats.BaseAgility < prereqs.Agi)
            missing["AGI"] = prereqs.Agi - stats.BaseAgility;

        if (stats.BaseEndurance < prereqs.End)
            missing["END"] = prereqs.End - stats.BaseEndurance;

        if (stats.BaseWill < prereqs.Wil)
            missing["WIL"] = prereqs.Wil - stats.BaseWill;

        return missing;
    }
}
