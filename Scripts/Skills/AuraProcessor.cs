using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Skills;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes aura skills, applying effects to entities within range.
/// Updates aura targets when entities move or when the caster moves.
/// </summary>
public partial class AuraProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private DataLoader? _dataLoader;
    private EntityManager? _entityManager;
    private TurnManager? _turnManager;

    /// <summary>
    /// Tracks which auras are currently active and their affected entities.
    /// Key: skill ID, Value: set of affected entity IDs
    /// </summary>
    private readonly Dictionary<string, HashSet<int>> _activeAuras = new();

    /// <summary>
    /// Cache of aura skill definitions for quick lookup.
    /// </summary>
    private readonly Dictionary<string, SkillDefinition> _auraSkillCache = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("AuraProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");

        if (_skillComponent == null)
        {
            GD.PushWarning("AuraProcessor: SkillComponent not found");
            return;
        }

        // Connect to skill learned signal
        _skillComponent.Connect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));

        // Connect to entity position changes to update auras
        _entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnPositionChanged));

        // Deferred initialization for systems that may not be ready yet
        CallDeferred(nameof(DeferredInit));
    }

    /// <summary>
    /// Deferred initialization to find game systems.
    /// </summary>
    private void DeferredInit()
    {
        // Find DataLoader
        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");

        // Find EntityManager and TurnManager by traversing up to GameLevel
        Node? current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _entityManager = current.GetNodeOrNull<EntityManager>("EntityManager");
                _turnManager = current.GetNodeOrNull<TurnManager>("TurnManager");
                break;
            }
            current = current.GetParent();
        }

        // Connect to turn signal to update auras each turn
        if (_turnManager != null && _entity is Player)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }

        // Initialize auras for any already-learned skills
        InitializeExistingAuras();
    }

    public override void _ExitTree()
    {
        if (_skillComponent != null && IsInstanceValid(_skillComponent))
        {
            _skillComponent.Disconnect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));
        }

        if (_entity != null && IsInstanceValid(_entity))
        {
            _entity.Disconnect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnPositionChanged));
        }

        if (_turnManager != null && IsInstanceValid(_turnManager))
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }

        // Remove all aura effects on cleanup
        RemoveAllAuraEffects();
    }

    /// <summary>
    /// Called when a skill is learned. Initializes aura if it's an aura skill.
    /// </summary>
    private void OnSkillLearned(string skillId)
    {
        if (_dataLoader == null) return;

        var skill = _dataLoader.GetSkill(skillId);
        if (skill == null || skill.GetCategory() != SkillCategory.Aura)
            return;

        InitializeAura(skill);
    }

    /// <summary>
    /// Called when the entity moves. Updates aura targets.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        UpdateAllAuras();
    }

    /// <summary>
    /// Called at the start of each turn. Updates aura targets.
    /// </summary>
    private void OnTurnStarted()
    {
        UpdateAllAuras();
    }

    /// <summary>
    /// Initializes auras for any already-learned aura skills.
    /// </summary>
    private void InitializeExistingAuras()
    {
        if (_skillComponent == null || _dataLoader == null) return;

        var learnedSkills = _skillComponent.GetLearnedSkillsByCategory(_dataLoader);
        if (!learnedSkills.TryGetValue(SkillCategory.Aura, out var auraSkills))
            return;

        foreach (var skill in auraSkills)
        {
            InitializeAura(skill);
        }
    }

    /// <summary>
    /// Initializes a single aura skill.
    /// </summary>
    private void InitializeAura(SkillDefinition skill)
    {
        if (_activeAuras.ContainsKey(skill.Id))
            return;

        _auraSkillCache[skill.Id] = skill;
        _activeAuras[skill.Id] = new HashSet<int>();

        GD.Print($"AuraProcessor: Initialized aura '{skill.Name}' (radius: {skill.AuraRadius}, target: {skill.AuraTarget})");

        // Immediately update this aura to affect nearby entities
        UpdateAura(skill);
    }

    /// <summary>
    /// Updates all active auras, applying/removing effects as needed.
    /// </summary>
    private void UpdateAllAuras()
    {
        foreach (var skillId in _activeAuras.Keys.ToList())
        {
            if (_auraSkillCache.TryGetValue(skillId, out var skill))
            {
                UpdateAura(skill);
            }
        }
    }

    /// <summary>
    /// Updates a single aura, applying effects to entities in range
    /// and removing effects from entities out of range.
    /// </summary>
    private void UpdateAura(SkillDefinition skill)
    {
        if (_entity == null || _entityManager == null) return;

        var currentlyAffected = _activeAuras[skill.Id];
        var shouldBeAffected = GetEntitiesInAuraRange(skill);

        // Find entities that left the aura
        var leftAura = currentlyAffected.Except(shouldBeAffected).ToList();
        foreach (var entityId in leftAura)
        {
            var entity = _entityManager.GetEntityById(entityId);
            if (entity != null)
            {
                RemoveAuraEffectFromEntity(skill, entity);
            }
            currentlyAffected.Remove(entityId);
        }

        // Find entities that entered the aura
        var enteredAura = shouldBeAffected.Except(currentlyAffected).ToList();
        foreach (var entityId in enteredAura)
        {
            var entity = _entityManager.GetEntityById(entityId);
            if (entity != null)
            {
                ApplyAuraEffectToEntity(skill, entity);
                currentlyAffected.Add(entityId);
            }
        }
    }

    /// <summary>
    /// Gets IDs of entities within the aura's range that match its target type.
    /// </summary>
    private HashSet<int> GetEntitiesInAuraRange(SkillDefinition skill)
    {
        var result = new HashSet<int>();
        if (_entity == null || _entityManager == null) return result;

        var entities = _entityManager.GetEntitiesInRadius(_entity.GridPosition, skill.AuraRadius);

        foreach (var entity in entities)
        {
            // Skip self
            if (entity == _entity) continue;

            // Check if entity matches aura target type
            if (IsValidAuraTarget(skill, entity))
            {
                result.Add(entity.GetInstanceId().GetHashCode());
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if an entity is a valid target for the aura.
    /// </summary>
    private bool IsValidAuraTarget(SkillDefinition skill, BaseEntity target)
    {
        if (_entity == null) return false;

        string auraTarget = skill.AuraTarget?.ToLower() ?? "all";

        return auraTarget switch
        {
            "allies" => _entity.Faction.IsFriendlyTo(target.Faction) && target != _entity,
            "enemies" => !_entity.Faction.IsFriendlyTo(target.Faction),
            "all" => target != _entity,
            _ => target != _entity
        };
    }

    /// <summary>
    /// Applies the aura's effects to an entity.
    /// </summary>
    private void ApplyAuraEffectToEntity(SkillDefinition skill, BaseEntity target)
    {
        var conditionComponent = target.GetNodeOrNull<ConditionComponent>("ConditionComponent");
        if (conditionComponent == null) return;

        string source = GetAuraSource(skill.Id);

        foreach (var effect in skill.Effects)
        {
            ApplyAuraEffect(effect, target, source);
        }

        GD.Print($"AuraProcessor: Applied aura '{skill.Name}' to {target.DisplayName}");
    }

    /// <summary>
    /// Removes the aura's effects from an entity.
    /// </summary>
    private void RemoveAuraEffectFromEntity(SkillDefinition skill, BaseEntity target)
    {
        var conditionComponent = target.GetNodeOrNull<ConditionComponent>("ConditionComponent");
        if (conditionComponent == null) return;

        string source = GetAuraSource(skill.Id);

        // Remove aura-applied conditions by source
        // Note: This requires the condition system to track sources
        // For now, we'll remove by type ID matching our aura pattern
        var statsComponent = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (statsComponent != null)
        {
            statsComponent.RemoveStrengthModifier(source);
            statsComponent.RemoveAgilityModifier(source);
            statsComponent.RemoveEnduranceModifier(source);
            statsComponent.RemoveWillModifier(source);
            statsComponent.RemoveArmorSource(source);
            statsComponent.RemoveEvasionPenaltySource(source);
        }

        GD.Print($"AuraProcessor: Removed aura '{skill.Name}' from {target.DisplayName}");
    }

    /// <summary>
    /// Applies a single aura effect to a target.
    /// </summary>
    private void ApplyAuraEffect(SkillEffectDefinition effect, BaseEntity target, string source)
    {
        var statsComponent = target.GetNodeOrNull<StatsComponent>("StatsComponent");
        if (statsComponent == null) return;

        switch (effect.Type?.ToLower())
        {
            case "stat_bonus":
                ApplyAuraStatBonus(effect, statsComponent, source);
                break;

            case "armor_bonus":
                statsComponent.AddArmorSource(source, effect.Amount);
                break;

            case "attack_bonus":
                // Apply as strength modifier (affects melee attack)
                statsComponent.AddStrengthModifier(source, effect.Amount);
                break;

            case "debuff":
                // Apply negative stat modifier
                ApplyAuraDebuff(effect, statsComponent, source);
                break;

            default:
                GD.PushWarning($"AuraProcessor: Unknown aura effect type '{effect.Type}'");
                break;
        }
    }

    /// <summary>
    /// Applies a stat bonus aura effect.
    /// </summary>
    private void ApplyAuraStatBonus(SkillEffectDefinition effect, StatsComponent stats, string source)
    {
        string stat = effect.Stat?.ToLower() ?? string.Empty;
        int amount = effect.Amount;

        switch (stat)
        {
            case "str":
            case "strength":
                stats.AddStrengthModifier(source, amount);
                break;
            case "agi":
            case "agility":
                stats.AddAgilityModifier(source, amount);
                break;
            case "end":
            case "endurance":
                stats.AddEnduranceModifier(source, amount);
                break;
            case "wil":
            case "will":
            case "willpower":
                stats.AddWillModifier(source, amount);
                break;
            case "armor":
                stats.AddArmorSource(source, amount);
                break;
            case "attack":
                stats.AddStrengthModifier(source, amount);
                break;
        }
    }

    /// <summary>
    /// Applies an aura debuff (negative modifier).
    /// </summary>
    private void ApplyAuraDebuff(SkillEffectDefinition effect, StatsComponent stats, string source)
    {
        string stat = effect.Stat?.ToLower() ?? "attack";
        int amount = -Mathf.Abs(effect.Amount); // Ensure negative

        switch (stat)
        {
            case "str":
            case "strength":
            case "attack":
                stats.AddStrengthModifier(source, amount);
                break;
            case "agi":
            case "agility":
            case "defense":
                stats.AddAgilityModifier(source, amount);
                break;
        }
    }

    /// <summary>
    /// Removes all aura effects from all affected entities.
    /// </summary>
    private void RemoveAllAuraEffects()
    {
        foreach (var kvp in _activeAuras)
        {
            if (!_auraSkillCache.TryGetValue(kvp.Key, out var skill))
                continue;

            foreach (var entityId in kvp.Value)
            {
                if (_entityManager == null) continue;
                var entity = _entityManager.GetEntityById(entityId);
                if (entity != null)
                {
                    RemoveAuraEffectFromEntity(skill, entity);
                }
            }
        }

        _activeAuras.Clear();
        _auraSkillCache.Clear();
    }

    /// <summary>
    /// Gets the modifier source string for an aura skill.
    /// </summary>
    private string GetAuraSource(string skillId) => $"aura_{skillId}_{_entity?.GetInstanceId()}";
}
