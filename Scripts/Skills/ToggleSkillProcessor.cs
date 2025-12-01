using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes toggle skills, managing their on/off state and applying conditions.
/// Toggle skills provide benefits (stat bonuses) and penalties (stat penalties or WP drain) while active.
/// Multiple toggles can be active simultaneously.
/// </summary>
public partial class ToggleSkillProcessor : Node
{
    [Signal]
    public delegate void ToggleActivatedEventHandler(string skillId, string skillName);

    [Signal]
    public delegate void ToggleDeactivatedEventHandler(string skillId, string skillName);

    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private WillpowerComponent? _willpowerComponent;
    private DataLoader? _dataLoader;

    /// <summary>
    /// Tracks which toggle skills are currently active.
    /// </summary>
    private readonly HashSet<string> _activeToggles = new();

    /// <summary>
    /// Maps skill ID to WP drain per turn (for active toggles with drain).
    /// </summary>
    private readonly Dictionary<string, int> _toggleDrains = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("ToggleSkillProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");
        _willpowerComponent = _entity.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");

        if (_skillComponent == null)
        {
            GD.PushWarning("ToggleSkillProcessor: SkillComponent not found");
            return;
        }

        // Connect to WillpowerChanged to detect when WP is depleted
        if (_willpowerComponent != null)
        {
            _willpowerComponent.Connect(
                WillpowerComponent.SignalName.WillpowerChanged,
                Callable.From<int, int>(OnWillpowerChanged));
        }
    }

    public override void _ExitTree()
    {
        // Disconnect signals
        if (_willpowerComponent != null && IsInstanceValid(_willpowerComponent))
        {
            _willpowerComponent.Disconnect(
                WillpowerComponent.SignalName.WillpowerChanged,
                Callable.From<int, int>(OnWillpowerChanged));
        }

        // Deactivate all active toggles on cleanup
        DeactivateAllToggles();
    }

    /// <summary>
    /// Toggles a skill on or off.
    /// </summary>
    /// <param name="skillId">The skill ID to toggle</param>
    /// <returns>True if the skill is now active, false if now inactive</returns>
    public bool ToggleSkill(string skillId)
    {
        if (_activeToggles.Contains(skillId))
        {
            DeactivateToggle(skillId);
            return false;
        }
        else
        {
            ActivateToggle(skillId);
            return true;
        }
    }

    /// <summary>
    /// Checks if a toggle skill is currently active.
    /// </summary>
    public bool IsToggleActive(string skillId)
    {
        return _activeToggles.Contains(skillId);
    }

    /// <summary>
    /// Gets all currently active toggle skill IDs.
    /// </summary>
    public IReadOnlyCollection<string> GetActiveToggles()
    {
        return _activeToggles;
    }

    /// <summary>
    /// Called each turn to process WP drain for active toggles.
    /// Should be called by the turn system when the entity's turn starts.
    /// </summary>
    public void ProcessTurnDrain()
    {
        if (_willpowerComponent == null || _toggleDrains.Count == 0)
            return;

        int totalDrain = 0;
        foreach (var drain in _toggleDrains.Values)
        {
            totalDrain += drain;
        }

        if (totalDrain > 0)
        {
            _willpowerComponent.SpendWillpower(totalDrain);
            // WP depletion check happens in OnWillpowerChanged
        }
    }

    /// <summary>
    /// Activates a toggle skill, applying its conditions.
    /// </summary>
    private void ActivateToggle(string skillId)
    {
        if (_dataLoader == null || _entity == null)
            return;

        var skill = _dataLoader.Skills.Get(skillId);
        if (skill?.Toggle == null)
        {
            GD.PushWarning($"ToggleSkillProcessor: Skill '{skillId}' has no toggle config");
            return;
        }

        string source = GetToggleSource(skillId);

        // Apply all stat modifiers (positive = bonus, negative = penalty)
        foreach (var modifier in skill.Toggle.Modifiers)
        {
            ApplyStatModifier(modifier.Stat, modifier.Amount, source);
        }

        // Track WP drain if any
        if (skill.Toggle.WpDrainPerTurn > 0)
        {
            _toggleDrains[skillId] = skill.Toggle.WpDrainPerTurn;
        }

        _activeToggles.Add(skillId);
        EmitSignal(SignalName.ToggleActivated, skillId, skill.Name);
        GD.Print($"ToggleSkillProcessor: Activated '{skill.Name}'");
    }

    /// <summary>
    /// Deactivates a toggle skill, removing its conditions.
    /// </summary>
    private void DeactivateToggle(string skillId)
    {
        if (!_activeToggles.Contains(skillId))
            return;

        string source = GetToggleSource(skillId);
        _entity?.RemoveConditionsBySource(source);
        _toggleDrains.Remove(skillId);
        _activeToggles.Remove(skillId);

        var skill = _dataLoader?.Skills.Get(skillId);
        string skillName = skill?.Name ?? skillId;
        EmitSignal(SignalName.ToggleDeactivated, skillId, skillName);
        GD.Print($"ToggleSkillProcessor: Deactivated '{skillName}'");
    }

    /// <summary>
    /// Deactivates all active toggles.
    /// </summary>
    private void DeactivateAllToggles()
    {
        // Copy to avoid modification during iteration
        var toggles = new List<string>(_activeToggles);
        foreach (var skillId in toggles)
        {
            DeactivateToggle(skillId);
        }
    }

    /// <summary>
    /// Applies a stat modifier condition for a toggle.
    /// </summary>
    private void ApplyStatModifier(string stat, int amount, string source)
    {
        if (_entity == null)
            return;

        var conditionType = StatConditionMapper.GetConditionType(stat);
        if (conditionType == null)
        {
            GD.PushWarning($"ToggleSkillProcessor: Unknown stat type '{stat}'");
            return;
        }

        var condition = ConditionFactory.Create(
            conditionType,
            amount,
            "1", // Duration doesn't matter for WhileActive
            ConditionDuration.WhileActive,
            source
        );

        if (condition != null)
        {
            _entity.AddCondition(condition);
        }
    }

    /// <summary>
    /// Called when willpower changes. Auto-deactivates toggles if WP is depleted.
    /// </summary>
    private void OnWillpowerChanged(int current, int max)
    {
        if (current <= 0 && _activeToggles.Count > 0)
        {
            GD.Print("ToggleSkillProcessor: WP depleted, deactivating all toggles");
            DeactivateAllToggles();
        }
    }

    /// <summary>
    /// Gets the condition source ID for a toggle skill.
    /// </summary>
    private static string GetToggleSource(string skillId) => $"toggle_{skillId}";
}
