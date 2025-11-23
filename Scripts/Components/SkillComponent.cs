using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Skills;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages skills learned by an entity.
/// Tracks which skills are known and provides methods for learning and querying skills.
/// </summary>
public partial class SkillComponent : Node
{
    #region Signals

    /// <summary>
    /// Emitted when a skill is learned.
    /// </summary>
    [Signal]
    public delegate void SkillLearnedEventHandler(string skillId);

    #endregion

    #region Properties

    /// <summary>
    /// List of learned skill IDs.
    /// </summary>
    private readonly List<string> _learnedSkills = new();

    /// <summary>
    /// Number of skill points used (for tracking progression).
    /// </summary>
    public int SkillPointsUsed { get; private set; } = 0;

    /// <summary>
    /// Read-only access to learned skill IDs.
    /// </summary>
    public IReadOnlyList<string> LearnedSkills => _learnedSkills;

    #endregion

    #region Skill Management

    /// <summary>
    /// Learns a skill by ID.
    /// </summary>
    /// <param name="skillId">The skill ID to learn</param>
    /// <returns>True if the skill was learned, false if already known</returns>
    public bool LearnSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        if (_learnedSkills.Contains(skillId))
            return false;

        _learnedSkills.Add(skillId);
        SkillPointsUsed++;
        EmitSignal(SignalName.SkillLearned, skillId);
        GD.Print($"SkillComponent: Learned skill '{skillId}'");
        return true;
    }

    /// <summary>
    /// Checks if a skill is learned.
    /// </summary>
    /// <param name="skillId">The skill ID to check</param>
    /// <returns>True if the skill is known</returns>
    public bool HasSkill(string skillId)
    {
        return _learnedSkills.Contains(skillId);
    }

    /// <summary>
    /// Gets all available skills that can be learned (meets prereqs and not already known).
    /// </summary>
    /// <param name="stats">The entity's stats component</param>
    /// <param name="dataLoader">DataLoader to get skill definitions</param>
    /// <returns>List of skill definitions that can be learned</returns>
    public List<SkillDefinition> GetAvailableSkills(StatsComponent stats, DataLoader dataLoader)
    {
        var available = new List<SkillDefinition>();

        foreach (var skill in dataLoader.GetAllSkills())
        {
            if (!HasSkill(skill.Id) && PrerequisiteChecker.MeetsPrerequisites(skill, stats))
            {
                available.Add(skill);
            }
        }

        return available;
    }

    /// <summary>
    /// Checks if a skill can be learned (meets prereqs and not already known).
    /// </summary>
    /// <param name="skillId">The skill ID to check</param>
    /// <param name="stats">The entity's stats component</param>
    /// <param name="dataLoader">DataLoader to get skill definition</param>
    /// <returns>True if the skill can be learned</returns>
    public bool CanLearnSkill(string skillId, StatsComponent stats, DataLoader dataLoader)
    {
        if (HasSkill(skillId))
            return false;

        var skill = dataLoader.GetSkill(skillId);
        if (skill == null)
            return false;

        return PrerequisiteChecker.MeetsPrerequisites(skill, stats);
    }

    /// <summary>
    /// Gets learned skills grouped by category.
    /// </summary>
    /// <param name="dataLoader">DataLoader to get skill definitions</param>
    /// <returns>Dictionary of category -> list of skill definitions</returns>
    public Dictionary<SkillCategory, List<SkillDefinition>> GetLearnedSkillsByCategory(DataLoader dataLoader)
    {
        var grouped = new Dictionary<SkillCategory, List<SkillDefinition>>
        {
            [SkillCategory.Active] = new(),
            [SkillCategory.Passive] = new(),
            [SkillCategory.Reactive] = new(),
            [SkillCategory.Aura] = new()
        };

        foreach (var skillId in _learnedSkills)
        {
            var skill = dataLoader.GetSkill(skillId);
            if (skill != null)
            {
                grouped[skill.GetCategory()].Add(skill);
            }
        }

        return grouped;
    }

    /// <summary>
    /// Gets all learned skill definitions.
    /// </summary>
    /// <param name="dataLoader">DataLoader to get skill definitions</param>
    /// <returns>List of learned skill definitions</returns>
    public List<SkillDefinition> GetLearnedSkillDefinitions(DataLoader dataLoader)
    {
        var skills = new List<SkillDefinition>();

        foreach (var skillId in _learnedSkills)
        {
            var skill = dataLoader.GetSkill(skillId);
            if (skill != null)
            {
                skills.Add(skill);
            }
        }

        return skills;
    }

    /// <summary>
    /// Gets all learned active skills (skills that can be manually activated).
    /// </summary>
    /// <param name="dataLoader">DataLoader to get skill definitions</param>
    /// <returns>List of active skill definitions</returns>
    public List<SkillDefinition> GetLearnedActiveSkills(DataLoader dataLoader)
    {
        return GetLearnedSkillDefinitions(dataLoader)
            .Where(s => s.GetCategory() == SkillCategory.Active)
            .ToList();
    }

    /// <summary>
    /// Checks if a skill can be used (learned and WP available).
    /// </summary>
    /// <param name="skillId">The skill ID to check</param>
    /// <param name="dataLoader">DataLoader to get skill definition</param>
    /// <returns>True if the skill can be used</returns>
    public bool CanUseSkill(string skillId, DataLoader dataLoader)
    {
        if (!HasSkill(skillId))
            return false;

        var skill = dataLoader.GetSkill(skillId);
        if (skill == null)
            return false;

        // Only active skills can be used
        if (skill.GetCategory() != SkillCategory.Active)
            return false;

        // Check WP cost
        var parent = GetParent<BaseEntity>();
        var willpower = parent?.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        if (willpower == null)
            return false;

        return willpower.CurrentWillpower >= skill.WillpowerCost;
    }

    /// <summary>
    /// Gets skills that can currently be used (learned, active, sufficient WP).
    /// </summary>
    /// <param name="dataLoader">DataLoader to get skill definitions</param>
    /// <returns>List of usable skill definitions</returns>
    public List<SkillDefinition> GetUsableSkills(DataLoader dataLoader)
    {
        var parent = GetParent<BaseEntity>();
        var willpower = parent?.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        int currentWP = willpower?.CurrentWillpower ?? 0;

        return GetLearnedActiveSkills(dataLoader)
            .Where(s => s.WillpowerCost <= currentWP)
            .ToList();
    }

    #endregion
}
