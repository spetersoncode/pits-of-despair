using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Data;
using PitsOfDespair.Skills;
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

    #region Signals

    /// <summary>
    /// Emitted when a skill key is rebound.
    /// </summary>
    [Signal]
    public delegate void SkillKeyReboundEventHandler(char oldKey, char newKey, string skillName);

    #endregion

    #region Properties

    /// <summary>
    /// List of learned skill IDs.
    /// </summary>
    private readonly List<string> _learnedSkills = new();

    /// <summary>
    /// Skill slot assignments (key -> skillId) for active skills only.
    /// </summary>
    private readonly Dictionary<char, string> _skillSlots = new();

    /// <summary>
    /// Tracks which once_per_floor skills have been used on the current floor.
    /// Reset when descending to a new floor.
    /// </summary>
    private readonly HashSet<string> _usedThisFloor = new();

    /// <summary>
    /// Number of skill points used (for tracking progression).
    /// </summary>
    public int SkillPointsUsed { get; private set; } = 0;

    /// <summary>
    /// Read-only access to learned skill IDs.
    /// </summary>
    public IReadOnlyList<string> LearnedSkills => _learnedSkills;

    /// <summary>
    /// Read-only access to skill slot assignments.
    /// </summary>
    public IReadOnlyDictionary<char, string> SkillSlots => _skillSlots;

    #endregion

    #region Skill Management

    /// <summary>
    /// Learns a skill by ID.
    /// For active skills, auto-assigns the next available key slot.
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

        // Auto-assign slot for active skills
        var dataLoader = GetNode<DataLoader>("/root/DataLoader");
        var skill = dataLoader?.Skills.Get(skillId);
        if (skill != null && skill.GetCategory() == SkillCategory.Active)
        {
            AssignNextAvailableSlot(skillId);
        }

        EmitSignal(SignalName.SkillLearned, skillId);
        return true;
    }

    /// <summary>
    /// Assigns the next available key slot (a-z) to a skill.
    /// </summary>
    private void AssignNextAvailableSlot(string skillId)
    {
        for (char key = 'a'; key <= 'z'; key++)
        {
            if (!_skillSlots.ContainsKey(key))
            {
                _skillSlots[key] = skillId;
                return;
            }
        }
        // No available slots (extremely rare - 26+ active skills)
        GD.PushWarning($"SkillComponent: No available slot for skill '{skillId}'");
    }

    /// <summary>
    /// Gets the assigned key for a skill.
    /// </summary>
    /// <param name="skillId">The skill ID</param>
    /// <returns>The assigned key, or null if not assigned</returns>
    public char? GetSkillKey(string skillId)
    {
        foreach (var kvp in _skillSlots)
        {
            if (kvp.Value == skillId)
                return kvp.Key;
        }
        return null;
    }

    /// <summary>
    /// Gets the skill ID assigned to a key.
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <returns>The skill ID, or null if not assigned</returns>
    public string? GetSkillAtKey(char key)
    {
        return _skillSlots.TryGetValue(char.ToLower(key), out var skillId) ? skillId : null;
    }

    /// <summary>
    /// Rebinds a skill from one key to another.
    /// If the target key is occupied, swaps the skills.
    /// </summary>
    /// <param name="oldKey">Current key</param>
    /// <param name="newKey">Target key</param>
    /// <returns>True if rebind was successful</returns>
    public bool RebindSkillKey(char oldKey, char newKey)
    {
        oldKey = char.ToLower(oldKey);
        newKey = char.ToLower(newKey);

        if (!_skillSlots.TryGetValue(oldKey, out var skillId))
            return false;

        // Get skill name for signal
        var dataLoader = GetNode<DataLoader>("/root/DataLoader");
        var skill = dataLoader?.Skills.Get(skillId);
        string skillName = skill?.Name ?? skillId;

        // Same key - no change needed
        if (oldKey == newKey)
        {
            EmitSignal(SignalName.SkillKeyRebound, oldKey, newKey, skillName);
            return true;
        }

        // Check if target key is occupied
        if (_skillSlots.TryGetValue(newKey, out var swapSkillId))
        {
            // Swap: move the skill at newKey to oldKey
            _skillSlots[oldKey] = swapSkillId;
        }
        else
        {
            // Target key is free - just remove from old
            _skillSlots.Remove(oldKey);
        }

        // Assign skill to new key
        _skillSlots[newKey] = skillId;

        EmitSignal(SignalName.SkillKeyRebound, oldKey, newKey, skillName);
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

        foreach (var skill in dataLoader.Skills.GetAll())
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

        var skill = dataLoader.Skills.Get(skillId);
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
            [SkillCategory.Reactive] = new()
        };

        foreach (var skillId in _learnedSkills)
        {
            var skill = dataLoader.Skills.Get(skillId);
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
            var skill = dataLoader.Skills.Get(skillId);
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

        var skill = dataLoader.Skills.Get(skillId);
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

    #region Floor Cooldown Management

    /// <summary>
    /// Checks if a once_per_floor skill has been used on the current floor.
    /// </summary>
    /// <param name="skillId">The skill ID to check</param>
    /// <returns>True if the skill has been used this floor</returns>
    public bool IsSkillUsedThisFloor(string skillId)
    {
        return _usedThisFloor.Contains(skillId);
    }

    /// <summary>
    /// Marks a skill as used for the current floor.
    /// </summary>
    /// <param name="skillId">The skill ID to mark as used</param>
    public void MarkSkillUsedThisFloor(string skillId)
    {
        _usedThisFloor.Add(skillId);
    }

    /// <summary>
    /// Resets per-floor skill cooldowns. Call when descending to a new floor.
    /// </summary>
    public void ResetFloorCooldowns()
    {
        _usedThisFloor.Clear();
        GD.Print("SkillComponent: Floor cooldowns reset");
    }

    #endregion
}
