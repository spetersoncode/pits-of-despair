using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes passive skills, applying their permanent effects when learned.
/// Listens to SkillComponent.SkillLearned and applies effects via the condition system.
/// </summary>
public partial class PassiveSkillProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private DataLoader? _dataLoader;

    /// <summary>
    /// Tracks which passive skills have been applied (skill ID -> applied).
    /// Used to prevent double-application and for cleanup.
    /// </summary>
    private readonly HashSet<string> _appliedPassives = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("PassiveSkillProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");
        if (_skillComponent == null)
        {
            GD.PushWarning("PassiveSkillProcessor: SkillComponent not found");
            return;
        }

        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");
        if (_dataLoader == null)
        {
            GD.PushWarning("PassiveSkillProcessor: DataLoader not found");
            return;
        }

        // Connect to skill learned signal
        _skillComponent.Connect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));

        // Apply any already-learned passive skills (for save/load scenarios)
        ApplyExistingPassives();
    }

    public override void _ExitTree()
    {
        if (_skillComponent != null && IsInstanceValid(_skillComponent))
        {
            _skillComponent.Disconnect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));
        }

        // Remove all passive modifiers on cleanup
        RemoveAllPassiveModifiers();
    }

    /// <summary>
    /// Called when a skill is learned. Checks if passive and applies effects.
    /// </summary>
    private void OnSkillLearned(string skillId)
    {
        if (_dataLoader == null) return;

        var skill = _dataLoader.GetSkill(skillId);
        if (skill == null || skill.GetCategory() != SkillCategory.Passive)
            return;

        ApplyPassiveSkill(skill);
    }

    /// <summary>
    /// Applies effects from any already-learned passive skills.
    /// Called during initialization for save/load support.
    /// </summary>
    private void ApplyExistingPassives()
    {
        if (_skillComponent == null || _dataLoader == null) return;

        foreach (var skillId in _skillComponent.LearnedSkills)
        {
            var skill = _dataLoader.GetSkill(skillId);
            if (skill != null && skill.GetCategory() == SkillCategory.Passive)
            {
                ApplyPassiveSkill(skill);
            }
        }
    }

    /// <summary>
    /// Applies a passive skill's effects.
    /// </summary>
    private void ApplyPassiveSkill(SkillDefinition skill)
    {
        if (_appliedPassives.Contains(skill.Id))
        {
            GD.Print($"PassiveSkillProcessor: Skill '{skill.Id}' already applied");
            return;
        }

        string source = GetModifierSource(skill.Id);

        foreach (var effect in skill.Effects)
        {
            ApplyPassiveEffect(effect, source, skill);
        }

        _appliedPassives.Add(skill.Id);
        GD.Print($"PassiveSkillProcessor: Applied passive skill '{skill.Name}'");
    }

    /// <summary>
    /// Applies a single passive effect based on its type.
    /// All stat effects are routed through the condition system.
    /// </summary>
    private void ApplyPassiveEffect(SkillEffectDefinition effect, string source, SkillDefinition skill)
    {
        string? conditionType = null;
        int amount = effect.Amount;

        switch (effect.Type?.ToLower())
        {
            case "stat_bonus":
                // Use StatConditionMapper to convert stat name to condition type
                conditionType = StatConditionMapper.GetConditionType(effect.Stat);
                break;

            case "armor_bonus":
                conditionType = "armor_modifier";
                break;

            case "evasion_bonus":
                conditionType = "evasion_modifier";
                break;

            default:
                GD.PushWarning($"PassiveSkillProcessor: Unknown passive effect type '{effect.Type}' in skill '{skill.Id}'");
                return;
        }

        if (conditionType == null)
        {
            GD.PushWarning($"PassiveSkillProcessor: Unknown stat type '{effect.Stat}' in skill '{skill.Id}'");
            return;
        }

        ApplyCondition(conditionType, amount, source);
    }

    /// <summary>
    /// Creates and applies a permanent condition to the entity.
    /// </summary>
    private void ApplyCondition(string conditionType, int amount, string source)
    {
        if (_entity == null) return;

        var condition = ConditionFactory.Create(
            conditionType,
            amount,
            "1", // Duration doesn't matter for Permanent
            ConditionDuration.Permanent,
            source
        );

        if (condition != null)
        {
            _entity.AddCondition(condition);
        }
    }

    /// <summary>
    /// Removes a passive skill's effects (for unlearning, if ever implemented).
    /// </summary>
    public void RemovePassiveSkill(string skillId)
    {
        if (!_appliedPassives.Contains(skillId)) return;
        if (_dataLoader == null) return;

        var skill = _dataLoader.GetSkill(skillId);
        if (skill == null) return;

        string source = GetModifierSource(skillId);
        RemoveModifiersForSource(source);

        _appliedPassives.Remove(skillId);
        GD.Print($"PassiveSkillProcessor: Removed passive skill '{skill.Name}'");
    }

    /// <summary>
    /// Removes all modifiers for a given source.
    /// All modifiers are now handled via the condition system.
    /// </summary>
    private void RemoveModifiersForSource(string source)
    {
        _entity?.RemoveConditionsBySource(source);
    }

    /// <summary>
    /// Removes all passive modifiers (called on cleanup).
    /// </summary>
    private void RemoveAllPassiveModifiers()
    {
        foreach (var skillId in _appliedPassives)
        {
            string source = GetModifierSource(skillId);
            RemoveModifiersForSource(source);
        }
        _appliedPassives.Clear();
    }

    /// <summary>
    /// Gets the modifier source string for a skill.
    /// </summary>
    private static string GetModifierSource(string skillId) => $"skill_{skillId}";
}
