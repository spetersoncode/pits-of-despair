using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes passive skills, applying their permanent effects when learned.
/// Listens to SkillComponent.SkillLearned and registers stat modifiers.
/// </summary>
public partial class PassiveSkillProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private StatsComponent? _statsComponent;
    private WillpowerComponent? _willpowerComponent;
    private HealthComponent? _healthComponent;
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
        _statsComponent = _entity.GetNodeOrNull<StatsComponent>("StatsComponent");
        _willpowerComponent = _entity.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        _healthComponent = _entity.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (_skillComponent == null)
        {
            GD.PushWarning("PassiveSkillProcessor: SkillComponent not found");
            return;
        }

        // Find DataLoader
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
    /// </summary>
    private void ApplyPassiveEffect(SkillEffectDefinition effect, string source, SkillDefinition skill)
    {
        switch (effect.Type?.ToLower())
        {
            case "stat_bonus":
                ApplyStatBonus(effect, source);
                break;

            case "armor_bonus":
                ApplyArmorBonus(effect, source);
                break;

            case "evasion_bonus":
                ApplyEvasionBonus(effect, source);
                break;

            default:
                GD.PushWarning($"PassiveSkillProcessor: Unknown passive effect type '{effect.Type}' in skill '{skill.Id}'");
                break;
        }
    }

    /// <summary>
    /// Applies a stat bonus effect (str, agi, end, wil, max_hp, max_wp, armor, evasion).
    /// </summary>
    private void ApplyStatBonus(SkillEffectDefinition effect, string source)
    {
        if (_statsComponent == null) return;

        // Use the Stat property from YAML (e.g., "stat: max_hp")
        string stat = effect.Stat?.ToLower() ?? string.Empty;
        int amount = effect.Amount;

        switch (stat)
        {
            case "str":
            case "strength":
                _statsComponent.AddStrengthModifier(source, amount);
                break;

            case "agi":
            case "agility":
                _statsComponent.AddAgilityModifier(source, amount);
                break;

            case "end":
            case "endurance":
                _statsComponent.AddEnduranceModifier(source, amount);
                break;

            case "wil":
            case "will":
            case "willpower":
                _statsComponent.AddWillModifier(source, amount);
                break;

            case "max_hp":
                // HP bonus through END modifier would be quadratic
                // Instead, add direct HP bonus to HealthComponent
                ApplyMaxHPBonus(source, amount);
                break;

            case "max_wp":
                ApplyMaxWPBonus(source, amount);
                break;

            case "armor":
                _statsComponent.AddArmorSource(source, amount);
                break;

            case "evasion":
                // Positive evasion = reduce penalty (since penalties are negative)
                _statsComponent.AddEvasionPenaltySource(source, amount);
                break;

            default:
                GD.PushWarning($"PassiveSkillProcessor: Unknown stat type '{stat}' for stat_bonus");
                break;
        }
    }

    /// <summary>
    /// Applies an armor bonus effect.
    /// </summary>
    private void ApplyArmorBonus(SkillEffectDefinition effect, string source)
    {
        _statsComponent?.AddArmorSource(source, effect.Amount);
    }

    /// <summary>
    /// Applies an evasion bonus effect.
    /// </summary>
    private void ApplyEvasionBonus(SkillEffectDefinition effect, string source)
    {
        // Positive value improves evasion (counters penalties)
        _statsComponent?.AddEvasionPenaltySource(source, effect.Amount);
    }

    /// <summary>
    /// Applies a max HP bonus directly to HealthComponent.
    /// </summary>
    private void ApplyMaxHPBonus(string source, int amount)
    {
        _healthComponent?.AddMaxHPModifier(source, amount);
    }

    /// <summary>
    /// Applies a max WP bonus directly to WillpowerComponent.
    /// </summary>
    private void ApplyMaxWPBonus(string source, int amount)
    {
        _willpowerComponent?.AddMaxWPModifier(source, amount);
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
    /// </summary>
    private void RemoveModifiersForSource(string source)
    {
        _statsComponent?.RemoveStrengthModifier(source);
        _statsComponent?.RemoveAgilityModifier(source);
        _statsComponent?.RemoveEnduranceModifier(source);
        _statsComponent?.RemoveWillModifier(source);
        _statsComponent?.RemoveArmorSource(source);
        _statsComponent?.RemoveEvasionPenaltySource(source);
        _healthComponent?.RemoveMaxHPModifier(source);
        _willpowerComponent?.RemoveMaxWPModifier(source);
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
