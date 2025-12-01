using Godot;
using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes improvement skills, tracking which skills have improvements applied.
/// When an improvement skill is learned, it permanently modifies the target skill's properties.
/// </summary>
public partial class ImprovementSkillProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private DataLoader? _dataLoader;

    /// <summary>
    /// Maps base skill ID to list of improvement skill IDs that have been applied.
    /// </summary>
    private readonly Dictionary<string, List<string>> _appliedImprovements = new();

    /// <summary>
    /// Cache of combined modifiers per base skill.
    /// </summary>
    private readonly Dictionary<string, SkillModifiers> _modifierCache = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("ImprovementSkillProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");
        if (_skillComponent == null)
        {
            GD.PushWarning("ImprovementSkillProcessor: SkillComponent not found");
            return;
        }

        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");
        if (_dataLoader == null)
        {
            GD.PushWarning("ImprovementSkillProcessor: DataLoader not found");
            return;
        }

        // Connect to skill learned signal
        _skillComponent.Connect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));

        // Apply existing improvements (for save/load scenarios)
        ApplyExistingImprovements();
    }

    public override void _ExitTree()
    {
        if (_skillComponent != null && IsInstanceValid(_skillComponent))
        {
            _skillComponent.Disconnect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));
        }
    }

    /// <summary>
    /// Gets the combined modifiers for a skill from all applied improvements.
    /// </summary>
    /// <param name="baseSkillId">The skill ID to get modifiers for</param>
    /// <returns>Combined modifiers, or null if no improvements applied</returns>
    public SkillModifiers? GetModifiers(string baseSkillId)
    {
        return _modifierCache.TryGetValue(baseSkillId, out var mods) ? mods : null;
    }

    /// <summary>
    /// Gets the effective range for a skill, including improvements.
    /// </summary>
    public int GetEffectiveRange(SkillDefinition skill)
    {
        var mods = GetModifiers(skill.Id);
        return skill.Range + (mods?.Range ?? 0);
    }

    /// <summary>
    /// Gets the effective radius for a skill, including improvements.
    /// </summary>
    public int GetEffectiveRadius(SkillDefinition skill)
    {
        var mods = GetModifiers(skill.Id);
        return skill.Radius + (mods?.Radius ?? 0);
    }

    /// <summary>
    /// Gets the effective willpower cost for a skill, including improvements.
    /// </summary>
    public int GetEffectiveWillpowerCost(SkillDefinition skill)
    {
        var mods = GetModifiers(skill.Id);
        int cost = skill.WillpowerCost + (mods?.WillpowerCost ?? 0);
        return Mathf.Max(0, cost); // Can't go below 0
    }

    /// <summary>
    /// Gets the bonus damage from improvements for a skill.
    /// </summary>
    public int GetBonusDamage(string skillId)
    {
        var mods = GetModifiers(skillId);
        return mods?.Damage ?? 0;
    }

    /// <summary>
    /// Gets the bonus knockback distance from improvements for a skill.
    /// </summary>
    public int GetBonusKnockbackDistance(string skillId)
    {
        var mods = GetModifiers(skillId);
        return mods?.KnockbackDistance ?? 0;
    }

    /// <summary>
    /// Checks if a skill has any improvements applied.
    /// </summary>
    public bool HasImprovements(string baseSkillId)
    {
        return _appliedImprovements.ContainsKey(baseSkillId) && _appliedImprovements[baseSkillId].Count > 0;
    }

    /// <summary>
    /// Gets the list of improvement skill IDs applied to a base skill.
    /// </summary>
    public IReadOnlyList<string> GetAppliedImprovements(string baseSkillId)
    {
        return _appliedImprovements.TryGetValue(baseSkillId, out var list)
            ? list
            : System.Array.Empty<string>();
    }

    /// <summary>
    /// Called when a skill is learned. Checks if it's an improvement and applies it.
    /// </summary>
    private void OnSkillLearned(string skillId)
    {
        if (_dataLoader == null) return;

        var skill = _dataLoader.Skills.Get(skillId);
        if (skill == null || skill.GetCategory() != SkillCategory.Improvement)
            return;

        ApplyImprovement(skill);
    }

    /// <summary>
    /// Applies an improvement skill to its target.
    /// </summary>
    private void ApplyImprovement(SkillDefinition improvement)
    {
        if (improvement.Improves == null || string.IsNullOrEmpty(improvement.Improves.TargetSkill))
        {
            GD.PushWarning($"ImprovementSkillProcessor: Improvement '{improvement.Id}' has no target skill");
            return;
        }

        string targetId = improvement.Improves.TargetSkill;

        // Debug: Log what we're getting from the skill definition
        var mods = improvement.Improves.Modifiers;
        GD.Print($"ImprovementSkillProcessor: '{improvement.Name}' modifiers - Range:{mods.Range}, Damage:{mods.Damage}, Radius:{mods.Radius}, WP:{mods.WillpowerCost}, Knockback:{mods.KnockbackDistance}");

        // Add to applied improvements list
        if (!_appliedImprovements.TryGetValue(targetId, out var improvements))
        {
            improvements = new List<string>();
            _appliedImprovements[targetId] = improvements;
        }

        if (!improvements.Contains(improvement.Id))
        {
            improvements.Add(improvement.Id);
            RebuildModifierCache(targetId);
            GD.Print($"ImprovementSkillProcessor: Applied '{improvement.Name}' to '{targetId}'");
        }
    }

    /// <summary>
    /// Rebuilds the modifier cache for a base skill.
    /// </summary>
    private void RebuildModifierCache(string baseSkillId)
    {
        if (_dataLoader == null) return;

        if (!_appliedImprovements.TryGetValue(baseSkillId, out var improvements) || improvements.Count == 0)
        {
            _modifierCache.Remove(baseSkillId);
            return;
        }

        var combined = new SkillModifiers();
        foreach (var impId in improvements)
        {
            var imp = _dataLoader.Skills.Get(impId);
            if (imp?.Improves?.Modifiers != null)
            {
                combined.Add(imp.Improves.Modifiers);
            }
        }

        _modifierCache[baseSkillId] = combined;
    }

    /// <summary>
    /// Applies improvements for already-learned skills (save/load support).
    /// </summary>
    private void ApplyExistingImprovements()
    {
        if (_skillComponent == null || _dataLoader == null) return;

        foreach (var skillId in _skillComponent.LearnedSkills)
        {
            var skill = _dataLoader.Skills.Get(skillId);
            if (skill != null && skill.GetCategory() == SkillCategory.Improvement)
            {
                ApplyImprovement(skill);
            }
        }
    }
}
